using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PowerBIServiceBackup.Helpers;
using PowerBIServiceBackup.Models;

namespace PowerBIServiceBackup
{
    public static class BackupPowerBI
    {
        private static TokenCredentials _tokenCredentials;
        public static TokenCredentials TokenCredentials
        {
            get
            {
                if(_tokenCredentials == null)
                {
                    //Retrieve the access credential
                    TokenCredentials tokenCredentials = ADALHelper.GetToken(
                            ConfigurationManager.AppSettings["PowerBILogin"]
                           ,ConfigurationManager.AppSettings["PowerBIPassword"]
                           ,ConfigurationManager.AppSettings["AuthenticationContextUrl"]
                           ,ConfigurationManager.AppSettings["PowerBIRessourceUrl"]
                           ,ConfigurationManager.AppSettings["ClientId"]);

                    _tokenCredentials = tokenCredentials;
                }

                return _tokenCredentials;
            }
        }

        public static Uri PowerBIApiUrl = new Uri(ConfigurationManager.AppSettings["PowerBIApi"]);
        public static string BlobStorageCS = ConfigurationManager.AppSettings["BlobConnectionString"];
        public static string BlobStorageContainerName = ConfigurationManager.AppSettings["BlobContainerName"];

        [FunctionName("RetrievePowerBIReports")]
        public static async Task Run(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"Starting RetrievePowerBIReports");
            
            //Get power bi groups id
            string[] powerBIGroups = await context.CallActivityAsync<string[]>("GetGroups",null);

            //Get all reports of all groups
            List<Task<List<GroupReport>>> groupsTasks = new List<Task<List<GroupReport>>>();
            foreach (string group in powerBIGroups)
            {
                Task<List<GroupReport>> task = context.CallActivityAsync<List<GroupReport>>("GetReports", group);
                groupsTasks.Add(task);
            }
            await Task.WhenAll(groupsTasks);

            List<GroupReport> powerBIReports = groupsTasks.SelectMany(t => t.Result).ToList();

            // Treat all reports in //
            List<Task<string>> reportsTasks = new List<Task<string>>();
            foreach (GroupReport groupReport in powerBIReports)
            {
                    Task<string> task = context.CallActivityAsync<string>("UploadBlob", groupReport);
                    reportsTasks.Add(task);
            }

            await Task.WhenAll(reportsTasks);
            log.LogInformation($"************* Backup end ***************");
        }

        [FunctionName("GetGroups")]
        public static string[] GetGroups([ActivityTrigger]DurableActivityContext GroupContext, ILogger log)
        {
            log.LogInformation($"Retrieving PowerBI Groups");
            
            using (PowerBIClient powerBIClient = new PowerBIClient(PowerBIApiUrl, TokenCredentials))
            {
                return powerBIClient.Groups.GetGroups().Value.Select(x => x.Id).ToArray();
            }
        }

        [FunctionName("GetReports")]
        public static List<GroupReport> GetReports([ActivityTrigger]string group, ILogger log)
        {
            using (PowerBIClient powerBIClient = new PowerBIClient(PowerBIApiUrl, TokenCredentials))
            {
                try
                {
                    return powerBIClient.Reports.GetReports(group).Value.Select(x => new GroupReport(group, x.Id)).ToList();
                }
                catch (Exception e)
                {
                    log.LogInformation($"Error : " + e.Message);
                    return new List<GroupReport>();
                }   
            }
        }

        [FunctionName("UploadBlob")]
        public static async Task<string> UploadBlob([ActivityTrigger] GroupReport groupReport, ILogger log)
        {
            log.LogInformation("Trying to upload report {reportid} of group {groupid}", groupReport.ReportId, groupReport.GroupId);

            Stream reportStream;
            string reportName;
            try
            {
                // Create a Power BI Client object. It will be used to call Power BI APIs.
                using (PowerBIClient powerBIClient = new PowerBIClient(PowerBIApiUrl, TokenCredentials))
                {
                    log.LogInformation($"powerBI client created");
                    Report report = await powerBIClient.Reports.GetReportAsync(groupReport.GroupId, groupReport.ReportId);
                    reportName = DateTime.Now.ToString("yyyyMMdd_HH") + "h/" + report.Name + ".pbix";
                    reportStream = await powerBIClient.Reports.ExportReportAsync(groupReport.GroupId, groupReport.ReportId);
                }

                // Retrieve destination blob reference
                CloudBlockBlob pbixBlob = BlobStorageHelper.GetBlob(
                    BlobStorageCS
                    ,BlobStorageContainerName
                    ,reportName);
                log.LogInformation("Blob reference retrieved for report {reportname}", reportName);

                await pbixBlob.UploadFromStreamAsync(reportStream);
            }
            catch (Exception e)
            {
                log.LogError(new EventId(666), e, "Error while trying to upload blob: {message}", e.Message);
                return e.Message;
            }

            return $"{reportName} successfully created!";
        }
    }
}

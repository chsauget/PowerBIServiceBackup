using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage.Blob;
using PowerBIServiceBackup.Helpers;
using PowerBIServiceBackup.Models;
using Microsoft.Extensions.DependencyInjection;

namespace PowerBIServiceBackup
{
	using Infrastructure;
	using Infrastructure.Config;

	public static class BackupPowerBI
	{
		private static readonly AppSettings Settings =
			ServiceProviderConfiguration.GetServiceProvider().GetService<AppSettings>();

        private static TokenCredentials _tokenCredentials;
        public static TokenCredentials TokenCredentials
        {
            get
            {
                if(_tokenCredentials == null)
                {
                    //Retrieve the access credential
                    TokenCredentials tokenCredentials = ADALHelper.GetToken(
						Settings.AzureAd.PowerBILogin,
						Settings.AzureAd.PowerBIPassword,
						Settings.AzureAd.AuthenticationContextUrl,
						Settings.AzureAd.PowerBIRessourceUrl,
						Settings.AzureAd.ClientId);

                    _tokenCredentials = tokenCredentials;
                }

                return _tokenCredentials;
            }
        }

        public static Uri PowerBIApiUrl = new Uri(Settings.PowerBIApi);
        public static string BlobStorageCS = Settings.BlobStorage.BlobConnectionString;
        public static string BlobStorageContainerName = Settings.BlobStorage.BlobContainerName;

        [FunctionName("RetrievePowerBIReports")]
        public static async Task Run(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"Starting RetrievePowerBIReports");
            
            //Get power bi groups id
            Dictionary<string,string> powerBIGroups = await context.CallActivityAsync<Dictionary<string, string>>("GetGroups",null);

            //Get all reports of all groups
            List<Task<List<GroupReport>>> groupsTasks = new List<Task<List<GroupReport>>>();
            foreach (KeyValuePair<string,string> group in powerBIGroups)
            {
                Task<List<GroupReport>> task = context.CallActivityAsync<List<GroupReport>>("GetReports", group);
                groupsTasks.Add(task);
            }
            await Task.WhenAll(groupsTasks);

            List<GroupReport> powerBIReports = groupsTasks.SelectMany(t => t.Result).ToList();

            // Treat all reports in //
            Parallel.ForEach(powerBIReports, new ParallelOptions { MaxDegreeOfParallelism = Settings.MaxDegreeOfParallelism.Value},
            groupReport =>
            {
                context.CallActivityAsync<string>("UploadBlob", groupReport);
            });
        }

        [FunctionName("GetGroups")]
        public static Dictionary<string,string> GetGroups([ActivityTrigger]DurableActivityContext GroupContext, ILogger log)
        {
            log.LogInformation($"Retrieving PowerBI Groups");
            
            using (PowerBIClient powerBIClient = new PowerBIClient(PowerBIApiUrl, TokenCredentials))
            {
                return powerBIClient.Groups.GetGroups().Value.Select(x => new { x.Id, x.Name }).ToDictionary(t => t.Id,t => t.Name);
            }
        }

        [FunctionName("GetReports")]
        public static List<GroupReport> GetReports([ActivityTrigger]KeyValuePair<string,string> group, ILogger log)
        {
            using (PowerBIClient powerBIClient = new PowerBIClient(PowerBIApiUrl, TokenCredentials))
            {
                try
                {
                    return powerBIClient.Reports.GetReports(group.Key).Value.Select(x => new GroupReport(group.Key, x.Id, group.Value)).ToList();
                }
                catch (Exception e)
                {
                    log.LogError($"Error : " + e.Message);
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
                    reportName = DateTime.Now.ToString("yyyyMMdd_HH") + "h/" + groupReport.GroupName + "/" + report.Name + ".pbix";
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

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

namespace PowerBIServiceBackup
{
    public static class BackupPowerBI
    {
        [FunctionName("RetrievePowerBIReports")]
        public static async Task<List<string>> Run(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"Starting RetrievePowerBIReports");

            //Retrieve the access credential
            TokenCredentials tokenCredentials = GetToken(ConfigurationManager.AppSettings["PowerBILogin"]
                   , ConfigurationManager.AppSettings["PowerBIPassword"]
                   , ConfigurationManager.AppSettings["AuthenticationContextUrl"]
                   , ConfigurationManager.AppSettings["PowerBIRessourceUrl"]
                   , ConfigurationManager.AppSettings["ClientId"]);

            var outputs = new List<string>();

            //Get power bi groups id
            string[] powerBIGroups = await context.CallActivityAsync<string[]>("GetGroups",null);


            //Foreach group, get powerbi reports id
            List<GroupReport> powerBIReports = new List<GroupReport>();
            foreach (string group in powerBIGroups)
            {
                powerBIReports.AddRange(await context.CallActivityAsync<List<GroupReport>>("GetReports", group));
            }

            List<Task<string>> parallelTasks = new List<Task<string>>();
           
            foreach (GroupReport groupReport in powerBIReports)
            {
                    Task<string> task = context.CallActivityAsync<string>("UploadBlob", groupReport);
                    parallelTasks.Add(task);
            }

            await Task.WhenAll(parallelTasks);
            log.LogInformation($"************* Backup end ***************");

            return outputs;
        }
        [FunctionName("GetGroups")]
        public static string[] GetGroups([ActivityTrigger]DurableActivityContext GroupContext, ILogger log)
        {
            log.LogInformation($"Retrieving PowerBI Groups");

            //Retrieve the access credential
            TokenCredentials tokenCredentials = GetToken(ConfigurationManager.AppSettings["PowerBILogin"]
                   , ConfigurationManager.AppSettings["PowerBIPassword"]
                   , ConfigurationManager.AppSettings["AuthenticationContextUrl"]
                   , ConfigurationManager.AppSettings["PowerBIRessourceUrl"]
                   , ConfigurationManager.AppSettings["ClientId"]);

            using (PowerBIClient powerBIClient = new PowerBIClient(new Uri(ConfigurationManager.AppSettings["PowerBIApi"]), tokenCredentials))
            {
                return powerBIClient.Groups.GetGroups().Value.Select(x => x.Id).ToArray();
            }


        }
        [FunctionName("GetReports")]
        public static List<GroupReport> GetReports([ActivityTrigger]string group, ILogger log)
        {
            //Retrieve the access credential
            TokenCredentials tokenCredentials = GetToken(ConfigurationManager.AppSettings["PowerBILogin"]
                   , ConfigurationManager.AppSettings["PowerBIPassword"]
                   , ConfigurationManager.AppSettings["AuthenticationContextUrl"]
                   , ConfigurationManager.AppSettings["PowerBIRessourceUrl"]
                   , ConfigurationManager.AppSettings["ClientId"]);

            Dictionary<string, string> Reports = new Dictionary<string, string>();
            using (PowerBIClient powerBIClient = new PowerBIClient(new Uri(ConfigurationManager.AppSettings["PowerBIApi"]), tokenCredentials))
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
        public static string UploadBlob([ActivityTrigger] GroupReport groupReport, ILogger log)
        {
            //Retrieve the access credential
            TokenCredentials tokenCredentials = GetToken(ConfigurationManager.AppSettings["PowerBILogin"]
                   , ConfigurationManager.AppSettings["PowerBIPassword"]
                   , ConfigurationManager.AppSettings["AuthenticationContextUrl"]
                   , ConfigurationManager.AppSettings["PowerBIRessourceUrl"]
                   , ConfigurationManager.AppSettings["ClientId"]);

            Stream reportStream;
            string reportName;
            try
            {
                // Create a Power BI Client object. It will be used to call Power BI APIs.
                using (PowerBIClient powerBIClient = new PowerBIClient(new Uri(ConfigurationManager.AppSettings["PowerBIApi"]), tokenCredentials))
                {
                    log.LogInformation($"powerBI client created");
                    Report report = powerBIClient.Reports.GetReport(groupReport.GroupId, groupReport.ReportId);
                    reportName = DateTime.Now.ToString("yyyyMMdd_HH") + "h/" + report.Name + ".pbix";
                    reportStream = powerBIClient.Reports.ExportReport(groupReport.GroupId, groupReport.ReportId);
                }

                // Retrieve destination blob reference

                CloudBlockBlob pbixBlob = GetBlob(ConfigurationManager.AppSettings["BlobConnectionString"]
                    , ConfigurationManager.AppSettings["BlobContainerName"]
                    , reportName);
                log.LogInformation($"Blob reference retrieved");

                pbixBlob.UploadFromStream(reportStream);
            }
            catch (Exception e)
            {
                log.LogInformation($"Error : " + e.Message);
                return e.Message;
            }
            return $"{reportName} successfully created!";
        }

        public static CloudBlockBlob GetBlob(string connectionString, string containerName,string blobName)
        {

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            container.CreateIfNotExists();

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            return blockBlob;

        }
        //Retrieve the ADAL Token  https://docs.microsoft.com/fr-fr/power-bi/developer/get-azuread-access-token#access-token-for-non-power-bi-users-app-owns-data
        public static TokenCredentials GetToken(string PowerBILogin,string PowerBIPassword, string AuthenticationContextUrl, string PowerBIRessourceUrl, string ClientId)
        {
            UserCredential credential = new UserPasswordCredential(PowerBILogin, PowerBIPassword);

            // Authenticate using created credentials
            AuthenticationContext authenticationContext = new AuthenticationContext(AuthenticationContextUrl);

            Task<AuthenticationResult> authenticationResultTask = authenticationContext.AcquireTokenAsync(PowerBIRessourceUrl, ClientId, credential);

            AuthenticationResult authenticationResult = authenticationResultTask.Result;
            if (authenticationResult == null)
            {
                throw new Exception("Authentication Failed.");
            }
            else
            {
                return new TokenCredentials(authenticationResult.AccessToken, "Bearer");
            }
                
        }

        public class GroupReport
        {
            private string groupId;
            private string reportId;

            public GroupReport(string groupId, string reportId)
            {
                this.GroupId = groupId;
                this.ReportId = reportId;
            }

            public string GroupId { get => groupId; set => groupId = value; }
            public string ReportId { get => reportId; set => reportId = value; }

            public override string ToString()
            {
                return base.ToString();
            }
        }

    }
}

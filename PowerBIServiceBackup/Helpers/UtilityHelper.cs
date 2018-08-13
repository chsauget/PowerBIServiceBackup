using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;

namespace PowerBIServiceBackup.Helpers
{
    public static class UtilityHelper
    {
        public static string CheckConfig()
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["PowerBILogin"]))
                return "Setting : PowerBILogin is empty or doesn't exist";
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["PowerBIPassword"]))
                return "Setting : PowerBIPassword is empty or doesn't exist";
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthenticationContextUrl"]))
                return "Setting : AuthenticationContextUrl is empty or doesn't exist";
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["PowerBIRessourceUrl"]))
                return "Setting : PowerBIRessourceUrl is empty or doesn't exist";
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["ClientId"]))
                return "Setting : ClientId is empty or doesn't exist";
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["PowerBIApi"]))
                return "Setting : PowerBIApi is empty or doesn't exist";
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["BlobContainerName"]))
                return "Setting : BlobContainerName is empty or doesn't exist";
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["BlobConnectionString"]))
                return "Setting : BlobConnectionString is empty or doesn't exist";
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["MaxDegreeOfParallelism"]))
                return "Setting : MaxDegreeOfParallelism is empty or doesn't exist";

            return null;
        }
    }
}

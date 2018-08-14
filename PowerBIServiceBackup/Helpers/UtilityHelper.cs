namespace PowerBIServiceBackup.Helpers
{
	using Infrastructure;
	using Infrastructure.Config;
	using Microsoft.Extensions.DependencyInjection;

	public static class UtilityHelper
    {
		private static readonly AppSettings Settings = ServiceProviderConfiguration.GetServiceProvider().GetService<AppSettings>();

        public static string CheckConfig()
        {
            if (string.IsNullOrEmpty(Settings.AzureAd.PowerBILogin))
                return "Setting : PowerBILogin is empty or doesn't exist";
            if (string.IsNullOrEmpty(Settings.AzureAd.PowerBIPassword))
                return "Setting : PowerBIPassword is empty or doesn't exist";
            if (string.IsNullOrEmpty(Settings.AzureAd.AuthenticationContextUrl))
                return "Setting : AuthenticationContextUrl is empty or doesn't exist";
            if (string.IsNullOrEmpty(Settings.AzureAd.PowerBIRessourceUrl))
                return "Setting : PowerBIRessourceUrl is empty or doesn't exist";
            if (string.IsNullOrEmpty(Settings.AzureAd.ClientId))
                return "Setting : ClientId is empty or doesn't exist";
            if (string.IsNullOrEmpty(Settings.PowerBIApi))
                return "Setting : PowerBIApi is empty or doesn't exist";
            if (string.IsNullOrEmpty(Settings.BlobStorage.BlobContainerName))
                return "Setting : BlobContainerName is empty or doesn't exist";
            if (string.IsNullOrEmpty(Settings.BlobStorage.BlobConnectionString))
                return "Setting : BlobConnectionString is empty or doesn't exist";
            if (Settings.MaxDegreeOfParallelism == null)
                return "Setting : MaxDegreeOfParallelism is empty or doesn't exist";

            return null;
        }
    }
}

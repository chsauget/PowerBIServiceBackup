namespace PowerBIServiceBackup.Infrastructure.Config
{
	public class AzureAd
	{
		public string AuthenticationContextUrl { get; set; }
		public string PowerBIRessourceUrl { get; set; }
		public string ClientId { get; set; }
		public string PowerBILogin { get; set; }
		public string PowerBIPassword { get; set; }
	}
}
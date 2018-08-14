namespace PowerBIServiceBackup.Infrastructure.Config
{
	public class AppSettings
	{
		public AzureAd AzureAd { get; set; }

		public BlobStorage BlobStorage { get; set; }

		public string PowerBIApi { get; set; }

		public int? MaxDegreeOfParallelism { get; set; }
	}
}
namespace PowerBIServiceBackup.Infrastructure
{
	using System;
	using System.IO;
	using Config;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.DependencyInjection;

	internal static class ServiceProviderConfiguration
	{

		private static readonly Lazy<IServiceProvider> ServiceProvider = new Lazy<IServiceProvider>(BuildServiceProvider);
		
		private static IServiceProvider BuildServiceProvider()
		{
			var services = new ServiceCollection();

			// treats config as a singleton
			var config = GetConfiguration();
			services.AddSingleton(config);

			// bind config to a custom class to avoid magic string. 
			var appSettings = GetAppSettings(config);
			services.AddSingleton(appSettings);

            return services.BuildServiceProvider();
        }


		private static IConfigurationRoot GetConfiguration()
		{
			var config = new ConfigurationBuilder()
						 .SetBasePath(Directory.GetCurrentDirectory())
						 // you might want to change this, to your real config file.
						 .AddJsonFile("sample_local.settings", true, true)
						 .AddEnvironmentVariables()
						 .Build();

			return config;
		}

		private static AppSettings GetAppSettings(IConfiguration config)
		{
			var appSettings = new AppSettings();
			config.Bind(appSettings);
			return appSettings;
		}

		public static IServiceProvider GetServiceProvider()
		{
			return ServiceProvider.Value;
		}
    }
}
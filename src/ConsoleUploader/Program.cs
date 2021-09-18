using GoogleDriveTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace ConsoleUploader
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();

            await serviceProvider.GetService<Application>().Run(args);

        }
        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(LogLevel.Debug);
            });
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();
            services.Configure<GoogleDriveConfig>(configuration.GetSection("GoogleDrive"));
            services.AddSingleton<GoogleDrive>();
            services.AddTransient<Application>();
        }
    }
}

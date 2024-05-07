using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Status_HyperpayFunction;
using Status_HyperpayFunction.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
[assembly: WebJobsStartup(typeof(Startup))]
namespace Status_HyperpayFunction
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            ConfigureServices(builder.Services);
        }
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                              .SetBasePath(Environment.CurrentDirectory)
                              .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                              .AddEnvironmentVariables()
                              .Build();

            //CommonConnection.DBConnection = KeyVaultService.GetSecret(configuration.GetSection("ClientId").Value, configuration.GetSection("ClientSecret").Value, configuration.GetSection("dBConnection").Value);
            //CommonConnection.HyperPayAPIUrl = configuration.GetSection("HyperPayAPIUrl").Value;
            //CommonConnection.x_Api_Key = configuration.GetSection("x_Api_Key").Value;
            //CommonConnection.private_Key = configuration.GetSection("private_Key").Value;

            CommonConnection.RestAPIURL = configuration.GetSection("RestAPIURL").Value;
        }
    }
}

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using RestSharp;
using Status_HyperpayFunction.Utils;
using System;

namespace Status_HyperpayFunction
{
    public class RetryFailedTransactions
    {
        /// <summary>
        /// for every 5 min based on timer table 
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [FunctionName("RetryFailedTransactions")]
        public void Run([TimerTrigger("*/5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            RetryFailedTxes(log);
        }

        private static void RetryFailedTxes(ILogger log)
        {
            var client = new RestClient(CommonConnection.RestAPIURL + "/api/v1/ExchangeTransaction/Fireblocks/RetryFailedTransactions");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            var responce = client.Execute(request);
            log.LogInformation($"responce Result is  (" + responce.Content + ")");
        }


    }
}



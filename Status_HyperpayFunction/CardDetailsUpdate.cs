using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using RestSharp;
using Status_HyperpayFunction.Utils;

namespace Status_HyperpayFunction
{
    public class CardDetailsUpdate
    {
        [FunctionName("CardDetailsUpdate")]
        public void Run([TimerTrigger("1/5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                var client = new RestClient(CommonConnection.RestAPIURL + "api/v1/UpdateCardDetails");
                var request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                var responce = client.Execute(request);
                log.LogInformation($"responce Result is  (" + responce.Content + ")");
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}

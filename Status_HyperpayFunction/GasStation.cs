using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;

namespace Status_HyperpayFunction
{
    public class GasStation
    {
        [FunctionName("GasStation")]
        public void Run([TimerTrigger("* * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            GetGasBalance(log);
        }

        private void GetGasBalance(ILogger log)
        {
            var fbEstimatedFee = GetEstimatedGasFee(log);
        }


        private static Root GetEstimatedGasFee(ILogger log)
        {
            log.LogInformation($"GetEstimatedGasFee Method Started at: {DateTime.Now}");
            var token = "Bearer ";
            var client = new RestClient("http://integration.southeastasia.cloudapp.azure.com/kraken-tst" + "/Fireblocks/GetAccountBalance?vaultAccountId=5628&wallet=TRX_TEST");
            var request = new RestRequest(Method.GET);
            request.AddHeader("authorization", token);
            IRestResponse response = client.Execute(request);


            JObject jsonObject = JObject.Parse(response.Content);

            var fbEstimatedFee = JsonConvert.DeserializeObject<Root>(jsonObject.ToString());

            //JObject dada = JObject.Parse(response.Content);

            //var fbEstimatedFee = JsonConvert.DeserializeObject<Root>(dada.ToString());
            //log.LogInformation($"GetEstimatedGasFee Method Responce is: {response.Content}");
            return fbEstimatedFee;
        }
        public class Root
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("total")]
            public string Total { get; set; }

            [JsonProperty("balance")]
            public string Balance { get; set; }

            [JsonProperty("lockedAmount")]
            public string LockedAmount { get; set; }

            [JsonProperty("available")]
            public string Available { get; set; }

            [JsonProperty("pending")]
            public string Pending { get; set; }

            [JsonProperty("frozen")]
            public string Frozen { get; set; }

            [JsonProperty("staked")]
            public string Staked { get; set; }

            [JsonProperty("blockHeight")]
            public string BlockHeight { get; set; }

            [JsonProperty("blockHash")]
            public string BlockHash { get; set; }
        }

    }
}



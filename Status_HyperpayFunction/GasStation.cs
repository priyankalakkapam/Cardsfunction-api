using MailKit.Net.Smtp;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;


namespace Status_HyperpayFunction
{
    public class GasStation
    {
        [FunctionName("GasStation")]
        public void Run([TimerTrigger("*/5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            GetGasBalance(log);
        }



        private static void GetGasBalance(ILogger log)
        {
            var token = "Bearer ";
            var client = new RestClient("https://integrationapi.exchangapay.com" + "/Fireblocks/GetAccountBalance?vaultAccountId=3&wallet=TRX");
            var request = new RestRequest(Method.GET);
            request.AddHeader("authorization", token);
            IRestResponse response = client.Execute(request);


            var afterslachremove = response.Content.Replace(@"\", "");

            afterslachremove = afterslachremove.Substring(1, afterslachremove.Length - 2);

            JObject jsonObject = JObject.Parse(afterslachremove);

            var fbEstimatedFee = JsonConvert.DeserializeObject<Root>(jsonObject.ToString());

            log.LogInformation($"gas responce: {response.Content}");

            if (Convert.ToDecimal(fbEstimatedFee.Available) <= 100)
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Sender Name", "sudhakiran.ziraff@gmail.com"));
                email.To.Add(new MailboxAddress("Receiver Name", "kiran@tlvfintech.com"));
                email.Subject = "URGENT FIll TRX";
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = "<b>Hi Team, Your TRX Gas Amount is Less than 100 Currently is (" + fbEstimatedFee.Available + "), Can you please Fill Before facing issue in Transactions</b>"
                };

                using (var smtp = new SmtpClient())
                {
                    smtp.Connect("smtp.gmail.com", 587, false);

                    // Note: only needed if the SMTP server requires authentication
                    smtp.Authenticate("sudhakiran.ziraff@gmail.com", "ocbf pjok uvgu zcsi");

                    smtp.Send(email);
                    smtp.Disconnect(true);
                }
            }
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



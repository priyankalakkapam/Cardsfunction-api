using MailKit.Net.Smtp;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Status_HyperpayFunction.Utils;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics.Metrics;

namespace Status_HyperpayFunction
{
    public class GasStation
    {
        /// <summary>
        /// */15 * * * *------> 15 Minutes 
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [FunctionName("GasStation")]
        public void Run([TimerTrigger("0 */8 * * *")] TimerInfo myTimer, ILogger log)
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

            var minamount = 170;
            DailyGasStation dailyGasStation = new DailyGasStation();
            dailyGasStation = GasStatationEmailTemplate();
            dailyGasStation.Counter = (dailyGasStation.Counter ?? 0) + 1;
            if (dailyGasStation.CreatedDate <= DateTime.UtcNow.AddHours(-10))
            {
                string cn = CommonConnection.DBConnection;
                SqlConnection sqlConnection = new(cn);
                string query = string.Format($"Insert into [Common].[GasTemplate] (Id,CreatedDate,Counter) values (NEWID(),GETUTCDATE()," + dailyGasStation.Counter + ")");
                SqlCommand cmd = new(query, sqlConnection);
                sqlConnection.Open();
                cmd.ExecuteNonQuery();
                sqlConnection.Close();

                if (Convert.ToDecimal(fbEstimatedFee.Available) < minamount)
                {
                    var email = new MimeMessage();
                    email.From.Add(new MailboxAddress("Sender Name", "apps@exchanga.com"));

                    var recipients = new List<MailboxAddress>
                                        {
                                            new MailboxAddress("Receiver One", "kiran@tlvfintech.com"),
                                            new MailboxAddress("Receiver Two", "Mithun@tlvfintech.com"),
                                        };
                    email.To.AddRange(recipients);

                    email.Subject = "Low Gas Amount";
                    email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                    {
                        Text = "<!DOCTYPE html>\r\n<html>\r\n  <head>\r\n    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\r\n    <style type=\"text/css\">\r\n      :root {\r\n        color-scheme: light dark;\r\n        supported-color-schemes: light dark;\r\n      }\r\n\r\n      body {\r\n        font-size: 16px;\r\n        padding: 0 25px 0 25px;\r\n        background: #fff;\r\n        font-family: Arial, Helvetica, sans-serif;\r\n      }\r\n\r\n      a {\r\n        text-decoration: none !important\r\n      }\r\n\r\n      .table-radius {\r\n        border-radius: 50px !important\r\n      }\r\n\r\n      @media (prefers-color-scheme: dark) {\r\n        .dark-img {\r\n          display: block !important;\r\n        }\r\n\r\n        .light-img {\r\n          display: none !important;\r\n        }\r\n      }\r\n\r\n      .dark-img {\r\n        display: none !important;\r\n      }\r\n\r\n      [data-ogsc] .dark-img {\r\n        display: block !important;\r\n      }\r\n\r\n      [data-ogsc] .light-img {\r\n        display: none !important;\r\n      }\r\n    </style>\r\n  </head>\r\n  <body>\r\n    <table width=\"600\" style=\"padding:30px 60px;border-style:none;background-repeat:no-repeat;font-family:Arial,Helvetica,sans-serif;margin:auto;background-size:100% 100%;padding-bottom:60px;min-height: 450px;position: relative;\" cellpadding=\"0\" cellspacing=\"0\">\r\n      <tbody>\r\n        <tr>\r\n          <td style=\"text-align: right;\">\r\n            <img src=\"https://prduximagestorage.blob.core.windows.net/suissebaseimages/top-arrows.png\" width=\"80px\" alt=\"Image\" style=\"text-align:right;position:absolute;right:0;\" />\r\n          </td>\r\n        </tr>\r\n\t\t\r\n        <tr>\r\n        <td colspan=\"2\">\r\n\r\n          <table width=\"100%\" cellpadding=\"4px\">\r\n\t\t  <tr>\r\n\t\t<td colspan=\"2\">\r\n\t\t <p style=\"font-size:16px;margin: 0;\">Hi Team, your TRX gas amount is currently less than {{minamount}}. Can you please refill it to avoid any issues with transactions</p>\r\n\t\t <br>\r\n\t\r\n\t\t </td>\r\n\t\t</tr>\r\n            <tr>\r\n              <th style=\"text-align: left;\">Current Available Gas Amount:</th>\r\n              <td>{{Amount}}</td>\r\n            </tr>\r\n          \r\n          </table>\r\n        \r\n        </td>\r\n        </tr>\r\n\t\t\r\n        <tr>\r\n          <td>\r\n            <img src=\"https://prduximagestorage.blob.core.windows.net/suissebaseimages/bottom-arrow.png\" width=\"80px\" alt=\"Image\" style=\"text-align:right;position:absolute;bottom:0;\" />\r\n          </td>\r\n        </tr>\r\n      </tbody>\r\n    </table>\r\n  </body>\r\n</html>".Replace("{{Amount}}", fbEstimatedFee.Available).Replace("{{minamount}}", minamount.ToString())
                    };

                    if (Convert.ToDecimal(fbEstimatedFee.Available) < minamount)
                    {
                        using (var smtp = new SmtpClient())
                        {
                            smtp.Connect("smtp.gmail.com", 587, false);

                            // Note: only needed if the SMTP server requires authentication
                            smtp.Authenticate("apps@exchanga.com", "pmoy gjqx kwnd qeey");

                            smtp.Send(email);
                            smtp.Disconnect(true);
                        }
                    }
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

        private static DailyGasStation GasStatationEmailTemplate()
        {
            DailyGasStation dailyGasStation = new DailyGasStation();
            string query = "SELECT Counter, CreatedDate FROM [Common].[GasTemplate]";

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(CommonConnection.DBConnection))
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.Text;
                    sqlConnection.Open();

                    using (SqlDataReader dr = sqlCommand.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            dailyGasStation.Counter = dr["Counter"] != DBNull.Value ? Convert.ToInt32(dr["Counter"]) : 0;
                            dailyGasStation.CreatedDate = dr["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDate"]) : DateTime.MinValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                return null;
            }
            return dailyGasStation;
        }


        public class DailyGasStation
        {
            public DateTime? CreatedDate { get; set; }
            public int? Counter { get; set; }
        }
    }
}



using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MimeKit;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestSharp;
using Status_HyperpayFunction.Utils;
using MailKit.Net.Smtp;
using System.Linq;

namespace Status_HyperpayFunction
{
    /// for every 5 min based on timer table 
    public class MerchantBalance
    {
        [FunctionName("MerchantBalance")]
        public void Run([TimerTrigger("*/5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            MerchantCardBalance(log);
        }
        private static void MerchantCardBalance(ILogger log)
        {
            var client = new RestClient(CommonConnection.RestAPIURL + "api/v1/Merchant/Balance/usdt");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", ParameterType.RequestBody);


            var responce = client.Execute(request);


            HyperMerchantBalance hyperPayCard = JsonConvert.DeserializeObject<HyperMerchantBalance>(responce.Content);
            var coinBalance = hyperPayCard.data.Where(a => a.coin.ToUpper() == "USDT").FirstOrDefault();
            string coinAmount = coinBalance.amount;
            log.LogInformation($"Merchant Balance responce: {responce.Content}");

            var minamount = 150;
            if (Convert.ToDecimal(coinAmount) < minamount)
            {

                var dailyGasStation = GasStatationEmailTemplate();

                if (dailyGasStation.CreatedDate <= DateTime.UtcNow.AddHours(-8))
                {
                    var email = new MimeMessage();
                    email.From.Add(new MailboxAddress("ExchangaPay", "apps@exchanga.com"));
                    var recipients = new List<MailboxAddress>
                                        {
                                            new MailboxAddress("kiran", "kiran@tlvfintech.com"),
                                            new MailboxAddress("Mithun", "Mithun@tlvfintech.com"),
                                        };
                    email.To.AddRange(recipients);

                    email.Subject = "Low Merchant Balance";
                    email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                    {
                        Text = "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\r\n    <style type=\"text/css\">\r\n        :root {\r\n            color-scheme: light dark;\r\n            supported-color-schemes: light dark;\r\n        }\r\n\r\n        body {\r\n            font-size: 16px;\r\n            padding: 0 25px;\r\n            background: #fff;\r\n            font-family: Arial, Helvetica, sans-serif;\r\n        }\r\n\r\n        a {\r\n            text-decoration: none !important;\r\n        }\r\n\r\n        .table-radius {\r\n            border-radius: 50px !important;\r\n        }\r\n\r\n        @media (prefers-color-scheme: dark) {\r\n            .dark-img {\r\n                display: block !important;\r\n            }\r\n\r\n            .light-img {\r\n                display: none !important;\r\n            }\r\n        }\r\n\r\n        .dark-img {\r\n            display: none !important;\r\n        }\r\n\r\n        [data-ogsc] .dark-img {\r\n            display: block !important;\r\n        }\r\n\r\n        [data-ogsc] .light-img {\r\n            display: none !important;\r\n        }\r\n\r\n        .copy-button {\r\n            background-color: #4CAF50;\r\n            color: white;\r\n            border: none;\r\n            padding: 5px 10px;\r\n            text-align: center;\r\n            text-decoration: none;\r\n            display: inline-block;\r\n            font-size: 12px;\r\n            margin: 4px 2px;\r\n            cursor: pointer;\r\n            border-radius: 4px;\r\n        }\r\n    </style>\r\n</head>\r\n<body>\r\n    <table width=\"600\" style=\"padding:30px 60px;border-style:none;background-repeat:no-repeat;font-family:Arial,Helvetica,sans-serif;margin:auto;background-size:100% 100%;padding-bottom:60px;min-height: 450px;position: relative;\" cellpadding=\"0\" cellspacing=\"0\">\r\n        <tbody>\r\n            <tr>\r\n                <td style=\"text-align: right;\">\r\n                    <img src=\"https://prduximagestorage.blob.core.windows.net/suissebaseimages/top-arrows.png\" width=\"80px\" alt=\"Image\" style=\"text-align:right;position:absolute;right:0;\" />\r\n                </td>\r\n            </tr>\r\n            <tr>\r\n                <td colspan=\"2\">\r\n                    <table width=\"100%\" cellpadding=\"4px\">\r\n                        <tr>\r\n                            <td colspan=\"2\">\r\n                                <p style=\"font-size:16px;margin: 0;\">Hi Team, your Merchant Balance is currently less than 150 USDT. Can you please refill it to avoid any issues with transactions</p>\r\n                                <br>\r\n                            </td>\r\n                        </tr>\r\n                        <tr>\r\n                            <th style=\"text-align: left; white-space: nowrap;\">Current Available Merchant Balance</th>\r\n                            <td>: {{Amount}}</td>\r\n                        </tr>\r\n                        <tr>\r\n                            <th style=\"text-align: left;\">USDT (TRON) Address</th>\r\n                            <td>\r\n                                <p id=\"usdtAddress\" style=\"white-space: nowrap;\">: TN5hbMtcFxVa3rZF2DFBpaS1djpLaJ1siZ</p>\r\n                                <!-- <button class=\"copy-button\" onclick=\"copyToClipboard()\">Copy</button> -->\r\n                            </td>\r\n                        </tr>\r\n                    </table>\r\n                </td>\r\n            </tr>\r\n            <tr>\r\n                <td>\r\n                    <img src=\"https://prduximagestorage.blob.core.windows.net/suissebaseimages/bottom-arrow.png\" width=\"80px\" alt=\"Image\" style=\"text-align:right;position:absolute;bottom:0;\" />\r\n                </td>\r\n            </tr>\r\n        </tbody>\r\n    </table>\r\n\r\n    <script>\r\n        function copyToClipboard() {\r\n            var copyText = document.getElementById(\"usdtAddress\").innerText;\r\n            navigator.clipboard.writeText(copyText).then(function() {\r\n                alert(\"Address copied to clipboard\");\r\n            }, function(err) {\r\n                console.error(\"Could not copy text: \", err);\r\n            });\r\n        }\r\n    </script>\r\n</body>\r\n</html>\r\n".Replace("{{Amount}}", coinAmount)
                    };

                    if (Convert.ToDecimal(coinAmount) < minamount)
                    {
                        using (var smtp = new SmtpClient())
                        {
                            smtp.Connect("smtp.gmail.com", 587, false);

                            // Note: only needed if the SMTP server requires authentication
                            smtp.Authenticate("apps@exchanga.com", "pmoy gjqx kwnd qeey");

                            smtp.Send(email);
                            smtp.Disconnect(true);
                        }

                        //update counter

                        UpdateTimer(dailyGasStation);
                    }
                }
            }
        }
        public class Datum
        {
            public string coin { get; set; }
            public string amount { get; set; }
            public string total_amount { get; set; }
        }

        public class HyperMerchantBalance
        {
            public string code { get; set; }
            public string msg { get; set; }
            public List<Datum> data { get; set; }
        }


        private static DailyGasStation GasStatationEmailTemplate()
        {
            DailyGasStation dailyGasStation = new DailyGasStation();
            string query = "SELECT * FROM [Common].[Emailtimer] where Timerfor='MerchantBalance'";

            SqlConnection sqlConnection = new SqlConnection(CommonConnection.DBConnection);
            try
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(query, sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.Text;


                    using (SqlDataReader dr = sqlCommand.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            dailyGasStation.Timerfor = dr["Timerfor"].ToString();
                            dailyGasStation.Counter = Convert.ToInt32(dr["Counter"]);
                            dailyGasStation.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                        }
                    }
                }
                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                sqlConnection.Close();
                Console.WriteLine("An error occurred: " + ex.Message);
                return null;
            }
            return dailyGasStation;
        }

        private static void UpdateTimer(DailyGasStation dailyGasStation)
        {
            string cn = CommonConnection.DBConnection;
            SqlConnection sqlConnection = new(cn);
            try
            {
                string query = string.Format($"update [Common].[Emailtimer] set createddate=GETDATE(),counter='" + (dailyGasStation.Counter + 1) + "' where timerfor='MerchantBalance'");
                SqlCommand cmd = new(query, sqlConnection);
                sqlConnection.Open();
                cmd.ExecuteNonQuery();
                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                sqlConnection.Close();
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }


        public class DailyGasStation
        {
            public DateTime? CreatedDate { get; set; }
            public int? Counter { get; set; }
            public string Timerfor { get; set; }
        }
    }
}


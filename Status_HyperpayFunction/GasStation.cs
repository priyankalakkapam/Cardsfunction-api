using System;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Status_HyperpayFunction.Utils;
using Status_HyperpayFunction.ViewModels;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp.Authenticators;
using RestSharp;
using System.Net;

namespace Status_HyperpayFunction
{
    public class GasStation
    {
        [FunctionName("GasStation")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            CompanyConfiguration(log);
        }

        private async void CompanyConfiguration(ILogger log)
        {
            CompanyConfigration companyConfigration = new CompanyConfigration();
            string query = "select Name,ValueorJson from Common.TenantConfigurations where Name = 'CompanyConfiguration'";
            SqlConnection sqlConnection = new SqlConnection(CommonConnection.DBConnection);

            SqlCommand sqlCommand = new(query, sqlConnection);
            sqlConnection.Open();
            sqlCommand.CommandType = CommandType.Text;
            SqlDataReader dr = sqlCommand.ExecuteReader();
            string name = null;
            string valueorJson = null;
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    name = dr["Name"].ToString();
                    valueorJson = dr["ValueorJson"].ToString();
                }
            }
            sqlConnection.Close();
            List<CompanyConfigration> admimgasamount = JsonConvert.DeserializeObject<List<CompanyConfigration>>(name);
            var gasStation = admimgasamount.FirstOrDefault(a => a.AddressType.ToLower() == "gasStation" && a.NetworkId.ToUpper() == "TRX_TEST");
            decimal maxFeePerGasLimit = 300.0M;
            var fbEstimatedFee = GetEstimatedGasFee(log);
            if (fbEstimatedFee.maxFeePerGas < maxFeePerGasLimit)
            {
                await SendEmail(CommonConnection.AdminEmail, "Transaction at the gas station has been completed. Please fill the gas", "");
            }
        }


        private static EstimateGas GetEstimatedGasFee(ILogger log)
        {
            log.LogInformation($"GetEstimatedGasFee Method Started at: {DateTime.Now}");
            var token = "Bearer ";
            var client = new RestClient("" + "Fireblocks/FireBlockGasEstimateFee?assetId=trx");
            var request = new RestRequest(Method.GET);
            request.AddHeader("authorization", token);
            IRestResponse response = client.Execute(request);
            var fbEstimatedFee = JsonConvert.DeserializeObject<EstimateGas>(response.Content);
            log.LogInformation($"GetEstimatedGasFee Method Responce is: {response.Content}");
            return fbEstimatedFee;
        }


        private async Task SendEmail(string toEmail, string subject, string template)
        {
            Email emailClass = new Email();
            emailClass.IsHtml = true;
            emailClass.FromAddress = null;
            emailClass.ToAddress = toEmail;
            emailClass.Subject = subject;
            emailClass.Body = template;
            var isMailSend = SendMailgun(emailClass);
        }
        private object SendMailgun(Email email)
        {
            SqlConnection sqlConnection = new SqlConnection(CommonConnection.DBConnection);
            string query = "select APIKey,BaseUri,Domain,FromMail from [Configuration].[MailConfiguration] where Type = 'Mailgun'";
            SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);
            sqlConnection.Open();

            try
            {
                sqlCommand.CommandType = CommandType.Text;
                SqlDataReader dr = sqlCommand.ExecuteReader();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        email.BaseUri = dr["BaseUri"].ToString();
                        email.ApiKey = dr["APIKey"].ToString();
                        email.Domain = dr["Domain"].ToString();
                        email.FromAddress = dr["FromMail"].ToString();
                    }
                }
                sqlConnection.Close();
            }
            catch (Exception)
            {
                sqlConnection.Close();
                return false;
            }

            RestClient client = new RestClient();
            client.BaseUrl = new Uri(email.BaseUri);
            client.Authenticator = new HttpBasicAuthenticator("api", email.ApiKey);
            RestRequest request = new RestRequest();
            request.AddParameter("domain", email.Domain, ParameterType.UrlSegment);
            request.Resource = "/messages";
            request.AddParameter("from", email.FromAddress);
            request.AddParameter("to", email.ToAddress);
            email.IsHtml = true;
            if (!String.IsNullOrEmpty(email.BccAddress)) request.AddParameter("bcc", email.BccAddress);

            request.AddParameter("subject", email.Subject);

            if (email.IsHtml) request.AddParameter("html", email.Body);
            else request.AddParameter("text", email.Body);

            request.Method = Method.POST;
            IRestResponse response1 = client.Execute(request);

            if (response1.StatusCode == HttpStatusCode.OK)
                email.Responce = true;
            else
                email.Responce = false;

            return email.Responce;

        }
        public class EstimateGas
        {
            public decimal lowFeePerGas { get; set; }
            public decimal medFeePerGas { get; set; }
            public decimal highFeePerGas { get; set; }
            public decimal maxFeePerGas { get; set; }
            public decimal maxPriorityFeePerGas { get; set; }

            public decimal GasFeeInEthLow { get; set; }
            public decimal GasFeeInEthMed { get; set; }
            public decimal GasFeeInEthHigh { get; set; }
        }
    }
}



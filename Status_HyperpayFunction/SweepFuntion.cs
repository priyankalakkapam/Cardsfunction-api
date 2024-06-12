using System;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Status_HyperpayFunction.Utils;
using Newtonsoft.Json.Linq;
using RestSharp;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Status_HyperpayFunction
{
    public class SweepFuntion
    {
        // 0 */5 * * * * ----> 5 Minutes
        [FunctionName("SweepFuntion")]
        public void Run([TimerTrigger("*/5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            List<withdrawalTransactions> withdrawalTransactions = new();
            withdrawalTransactions = withdrawalPendingTransactions();

            foreach (var transactions in withdrawalTransactions)
            {
                var inputObj = JsonConvert.SerializeObject(transactions);
                log.LogInformation($"input Request is  (" + inputObj + ")at: {DateTime.Now}");

                var client = new RestClient(CommonConnection.RestAPIURL + "api/v1/ExchangeTransaction/Withdraw/Crypto/PendingTransactions");
                var request = new RestRequest(Method.PUT);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", inputObj, ParameterType.RequestBody);

                log.LogInformation($"input Request is  (" + inputObj + ")at: {DateTime.Now}");

                var responce = client.Execute(request);
                log.LogInformation($"responce Result is  (" + responce.Content + ")");
            }
            log.LogInformation($"Pending Transactions Completed at: {DateTime.Now}");



        }

        private static List<withdrawalTransactions> withdrawalPendingTransactions()
        {
            withdrawalTransactions withdrawalTransactions = new();
            List<withdrawalTransactions> listoftransactions = new();
            SqlConnection sqlConnection = new SqlConnection(CommonConnection.DBConnection);
            string query = "select CustomerId,Id,State,TxRef,IsCompleted,RetryCount from Finance.[Transaction] where TxType = 'Withdraw' and TxSubType = 'crypto' and State = 'Pending' and TxSource = 'Exchange' AND (RetryCount < 3 OR RetryCount IS NULL)  AND (IsCompleted = 0 OR IsCompleted IS NULL) order by TxDate desc";
            SqlCommand sqlCommand = new SqlCommand(query, sqlConnection);
            sqlConnection.Open();
            try
            {
                sqlCommand.CommandType = CommandType.Text;
                SqlDataAdapter da = new System.Data.SqlClient.SqlDataAdapter(sqlCommand);
                DataTable dt = new DataTable();
                da.Fill(dt);
                foreach (DataRow row in dt.Rows)
                {
                    withdrawalTransactions.CustomerId = Guid.Parse(row["CustomerId"].ToString());
                    withdrawalTransactions.Id = Guid.Parse(row["Id"].ToString());
                    withdrawalTransactions.State = row["State"].ToString();
                    listoftransactions.Add(withdrawalTransactions);
                }
                sqlConnection.Close();

                return listoftransactions;
            }
            catch (Exception)
            {
                sqlConnection.Close();
                return null;

            }
        }

        public class withdrawalTransactions
        {
            public Guid? Id { get; set; }
            public Guid? CustomerId { get; set; }
            public string State { get; set; }
        }
    }
}

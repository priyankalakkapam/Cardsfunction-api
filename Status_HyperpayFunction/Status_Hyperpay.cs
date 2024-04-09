using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Ocsp;
using RestSharp;
using Status_HyperpayFunction.Utils;
using Status_HyperpayFunction.ViewModels;

namespace Status_HyperpayFunction
{
    public class Status_Hyperpay
    {
        //0 */30 * * * *----> 30 Minutes

        [FunctionName("StatusUpdating")]
        public void Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var lstPendingCards = GetPendingCards(log);
            GetCardStatus(log, lstPendingCards);
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        private List<CardsListVm> GetPendingCards(ILogger log)
        {
            List<CardsListVm> cardsListVms = new List<CardsListVm>();
            SqlConnection connection = new SqlConnection(CommonConnection.DBConnection);
            connection.Open();
            try
            {
                using (SqlCommand sqlCommand = new SqlCommand("select Id,CustomerId,CardTradeNo,AccountHolderStatus,State from Member.CustomerWallet where Type ='Cards' and Provider = 'HyperPay' and State = 'Submitted' and CardTradeNo is not null", connection))
                {
                    sqlCommand.CommandType = CommandType.Text;
                    SqlDataAdapter da = new System.Data.SqlClient.SqlDataAdapter(sqlCommand);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                    {
                        CardsListVm _cardsListVm = new CardsListVm();

                        _cardsListVm.Id = Guid.Parse(row["Id"].ToString());
                        _cardsListVm.CustomerId = Guid.Parse(row["CustomerId"].ToString());
                        _cardsListVm.CardTradeNo = row["CardTradeNo"].ToString();
                        _cardsListVm.AccountHolderStatus = row["AccountHolderStatus"].ToString();
                        _cardsListVm.State = row["State"].ToString();
                        cardsListVms.Add(_cardsListVm);
                        log.LogInformation("CustomerId: " + _cardsListVm.CustomerId + ", accountHolderStatus" + _cardsListVm.AccountHolderStatus + ", State" + _cardsListVm.State);
                    }
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
            }
            return cardsListVms;
        }
        private void GetCardStatus(ILogger log, List<CardsListVm> lstCards)
        {
            foreach (var cards in lstCards)
            {
                HyperPayCardApplicationResult hyperPayCard = new();
                log.LogInformation("Entering the Excution query");

                if ((!string.IsNullOrEmpty(cards.AccountHolderStatus) && cards.AccountHolderStatus.ToLower() != "openingactivated") && cards.State.ToLower() == "submitted")
                {
                    string query = string.Empty;
                    string status = string.Empty;

                    var client = new RestClient(CommonConnection.HyperPayAPIUrl + "/api/v1/updatecardstatus/" + cards.CardTradeNo);
                    var request = new RestRequest(Method.POST);
                    request.AddParameter("application/json", ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        hyperPayCard = JsonConvert.DeserializeObject<HyperPayCardApplicationResult>(response.Content);

                        if (hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.OpeningReviewedSuccess))
                        {
                            status = "OpeningReviewedSuccess";
                            query = "update Member.CustomerWallet set AccountId=@card_id,AccountNumber=@card_no,AccountHolderStatus=@status where Id=@id";

                        }
                        else if (hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.ActivationReviewedSuccess) || hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.OpeningActivated))
                        {
                            HyperPayCardApplicationStatusEnum enumValue = (HyperPayCardApplicationStatusEnum)hyperPayCard.data.result.card_status;
                            status = enumValue.ToString();
                            query = $"update Member.CustomerWallet set AccountId=@card_id,AccountNumber=@card_no,AccountHolderStatus=@status,State='Approved' where Id=@id";
                        }
                        else if (hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.ActivationReviewedRejected) || hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.OpeningReviewedRejected))
                        {
                            status = hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.ActivationReviewedRejected) ? "ActivationReviewedRejected" : "OpeningReviewedRejected";
                            query = $"update Member.CustomerWallet set AccountHolderStatus=@status,State='Rejected' where Id=@id";
                        }
                        else
                        {
                            HyperPayCardApplicationStatusEnum enumValue = (HyperPayCardApplicationStatusEnum)hyperPayCard.data.result.card_status;
                            status = enumValue.ToString();
                            query = $"update Member.CustomerWallet set AccountHolderStatus=@status where Id=@id";
                        }
                        if (query != null)
                        {
                            query = query + ";" + "insert into Member.CustomerCardsOperations (Id,CustomerId,CardId,RequestNumber,ResponseStatus,CreatedDate,CreatedBy) values(@opId,@customerId,@card_id,@tradeNo,@status,@cDate,@CreatedBy)";
                            log.LogInformation("query: " + query);
                            FillExecuteQuery(cards, hyperPayCard, query, status, "System");
                            log.LogInformation("query executed successfully.");
                        }
                        Console.WriteLine(status);
                    }

                }
                //}
            }
        }
        private void FillExecuteQuery(CardsListVm cards, HyperPayCardApplicationResult hyperPayCard, string query, string status, string CreatedBy)
        {
            SqlConnection connection = new SqlConnection(CommonConnection.DBConnection);
            try
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@card_id", hyperPayCard.data.result.card_id);
                    cmd.Parameters.AddWithValue("@card_no", hyperPayCard.data.result.card_number);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", cards.Id);
                    cmd.Parameters.AddWithValue("@cDate", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@opId", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("@customerId", cards.CustomerId);
                    cmd.Parameters.AddWithValue("@tradeNo", cards.CardTradeNo);
                    cmd.Parameters.AddWithValue("@CreatedBy", "System");
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
            }
        }

        private string createNonce()
        {
            StringBuilder result = new StringBuilder();
            string[] letters = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q" };
            Random rand = new Random();
            for (int i = 0; i < 10; i++)
            {
                result.Append(letters[rand.Next(letters.Length)]);
            }
            return result.ToString();
        }






        private Dictionary<string, string> GetRequestBody(string request)
        {

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(request);
        }

        private string SignData(string data, string privateKey)
        {
            // Convert the private key to RSA parameters
            var privateKeyBytes = Convert.FromBase64String(privateKey);
            var rsa = RSA.Create();
            var rsaParameters = ConvertToRSAParameters(privateKeyBytes);
            rsa.ImportParameters(rsaParameters);

            // Sign the data
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Convert the signature to base64
            return Convert.ToBase64String(signatureBytes);
        }

        private RSAParameters ConvertToRSAParameters(byte[] privateKeyBytes)
        {
            var seq = Asn1Object.FromByteArray(privateKeyBytes) as DerSequence;
            if (seq == null || seq.Count != 9)
            {
                throw new ArgumentException("Invalid RSA private key");
            }

            var rsaParameters = new RSAParameters
            {
                Modulus = ((DerInteger)seq[1]).PositiveValue.ToByteArrayUnsigned(),
                Exponent = ((DerInteger)seq[2]).PositiveValue.ToByteArrayUnsigned(),
                D = ((DerInteger)seq[3]).PositiveValue.ToByteArrayUnsigned(),
                P = ((DerInteger)seq[4]).PositiveValue.ToByteArrayUnsigned(),
                Q = ((DerInteger)seq[5]).PositiveValue.ToByteArrayUnsigned(),
                DP = ((DerInteger)seq[6]).PositiveValue.ToByteArrayUnsigned(),
                DQ = ((DerInteger)seq[7]).PositiveValue.ToByteArrayUnsigned(),
                InverseQ = ((DerInteger)seq[8]).PositiveValue.ToByteArrayUnsigned(),
            };

            return rsaParameters;
        }
        private Dictionary<string, string> SortParameters(Dictionary<string, string> headers, Dictionary<string, string> body)
        {
            var combinedParameters = headers.Concat(body)
                                           .Where(x => !string.IsNullOrEmpty(x.Value))
                                           .OrderBy(x => x.Key);

            return combinedParameters.ToDictionary(x => x.Key, x => x.Value);
        }

        private string CombineParameters(Dictionary<string, string> parameters)
        {
            // Combine parameters into a string
            string combinedString = string.Join("&", parameters.Select(x => $"{x.Key}={x.Value}"));
            return combinedString;
        }
    }
}

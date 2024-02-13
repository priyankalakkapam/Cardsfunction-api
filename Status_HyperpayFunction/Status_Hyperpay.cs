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
        private string private_Key = "MIICXAIBAAKBgGA+Ae6AWAFjx4SiI2NGpOULZW1koHS8Cl00v2eJ0dfzyBBPx25R\r\nAoZe3upqZOTXZczhwb2BT3wet7yq1+pd4/ybYdxG2qSLo/O05+0XmcgUPUdwkdU6\r\necCsmZDqdbVgRaPOWhDPltfgnPza+1wLaRYq3KuhXRgx2B0URZ134PylAgMBAAEC\r\ngYAd4UKCRLCOBed840XvXZB2WBpuYy5576OcGnNOdviCfnpfrhUxx87r3uqAhvW6\r\nIrHFcVXQOyRtWbAb0ELmza2pbyglC+RQts28UJXqM9W2FYddWbCXr10lVh8dLhAx\r\nNrlTDorZHGbN4fJ8cf/b/nmF3kWYRSNEOTUJKugsIDjIYQJBAKE7Wn6QZt9y24ip\r\nxZmzvF63/vUwNbSgtKcjl7FzIgHKYBK5sEKSEy/HmdDwGfULfNayuOVKMStJM1oc\r\nIPNP4VkCQQCYz6Cx9ys58bgILkQn9D0qLC5WI+R/DkvoaqVtIaLrzhe8giXNwKjz\r\ngw9Qf2mdaUIDqQd5Aa+lxsic5InJXWAtAkEAjJVsOp8+k+dadLdTjMmjnhNhQ/ldWrolyvbF9fwl0tnbG3i9r84e3LJ19DDm8TurBqmffo5KgSu6kv+j24PzQQJAOm6K\r\nYALHgKyxVk96uFxoVwv12/J1mS/6TrEY+JX4GnsAEJEjq32UHSlsXbeaxxpMp+GmfdrrM1TDuVqaZWlTMQJBAII6O5A2Kg+uS8V2doTOk6SvN7bs175I8xfIxDFvwdNNvL5qcEjHQDbSueqv8iKEeZ4LUcazzDPet1N52wF6Pd8=";
        private string x_Api_Key = "cd989e1a-0646-460c-a362-a721eb63dea2";
        [FunctionName("StatusUpdateing")]
        public void Run([TimerTrigger("*/15 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var lstPendingCards = GetPendingCards(log);
            GetCardStatus(log, lstPendingCards);
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
        // https://doc-api.hyperpay.io/en/2api/21_hypercard_api/2129_application_result_v2.html    ---->  result

        ///https://doc-api.hyperpay.io/en/3appendix/33_card_application_status.html  ---> status

        // need to get data from member.customer table with cards submitted
        // check each card transaction status
        //update the card status in every loop 

        //if Opening – Reviewed - Success   continue  if Opening – Reviewed - Rejected stop that card

        //if Opening – Reviewed - Success call fireblocks
        //after fireblocks okay update the status and call payment 

        // after payment success then call 
        // activate the card

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
                //need to call fireblocks api
                //if (string.IsNullOrEmpty(cards.AccountHolderStatus) && cards.State.ToLower() == "submitted")
                //{
                //    var client = new HttpClient();
                //    var request = new HttpRequestMessage(HttpMethod.Get, $"https://neocard.azurewebsites.net/api/v1/paymentfireblockstocard/{cards.Id}/{cards.CustomerId}");
                //    var response = client.Send(request);
                //    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                //    {
                //        continue;
                //    }
                //    else
                //    {
                //        continue;
                //    }
                //}
                if (!string.IsNullOrEmpty(cards.AccountHolderStatus) && cards.State.ToLower() == "submitted")
                {

                    CardReqData cardReqData = new CardReqData();

                    cardReqData.mc_trade_no = cards.CardTradeNo;
                    long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string nonce = createNonce();
                    string req = JsonConvert.SerializeObject(cardReqData);
                    string sign = CreateSignature(req, timeStamp.ToString(), nonce);
                    IRestResponse response = FillRestAPIExecution(req, timeStamp, nonce, sign, "/v2/openapi/card/apply/result");
                    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        hyperPayCard = JsonConvert.DeserializeObject<HyperPayCardApplicationResult>(response.Content);
                        if (hyperPayCard.data != null && hyperPayCard.data.result != null && hyperPayCard.data.result.card_id != null)
                        {
                            string query = string.Empty;
                            string status = string.Empty;

                            if (hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.OpeningReviewedSuccess))
                            {
                                // activation call

                                if (hyperPayCard.data != null)
                                {
                                    HyperPayCardActivateRes res = new();
                                    HyperPayCardActivation cardActivation = new()
                                    {
                                        card_id = hyperPayCard.data.result.card_id,
                                    };
                                    timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                                    nonce = createNonce();
                                    req = JsonConvert.SerializeObject(cardActivation);
                                    sign = CreateSignature(req, timeStamp.ToString(), nonce);
                                    IRestResponse cardActivationRes = FillRestAPIExecution(req, timeStamp, nonce, sign, "/openapi/card/active");
                                    if (cardActivationRes.StatusCode == System.Net.HttpStatusCode.OK || cardActivationRes.StatusCode == System.Net.HttpStatusCode.Created)
                                    {
                                        res = JsonConvert.DeserializeObject<HyperPayCardActivateRes>(cardActivationRes.Content);
                                    }
                                }

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
                                query = query + ";" + "insert into Member.CustomerCardsOperations (Id,CustomerId,CardId,RequestNumber,ResponseStatus,CreatedDate) values(@opId,@customerId,@card_id,@tradeNo,@status,@cDate)";
                                FillExecuteQuery(cards, hyperPayCard, query, status);
                            }
                            Console.WriteLine(status);
                        }

                    }
                }
            }
        }
        private void FillExecuteQuery(CardsListVm cards, HyperPayCardApplicationResult hyperPayCard, string query, string status)
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
        private IRestResponse FillRestAPIExecution(string req, long timeStamp, string nonce, string sign, string url)
        {
            var client = new RestClient("https://sandbox.hyperpay.io");
            var request = new RestRequest(url, Method.POST);
            request.AddHeader("timestamp", timeStamp.ToString());
            request.AddHeader("nonce", nonce);
            request.AddHeader("api-key", x_Api_Key);
            request.AddHeader("signature", sign);
            request.AddHeader("version", "1.0");
            request.AddHeader("lang", "en");
            request.AddParameter("application/json", req, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return response;
        }

        private string CreateSignature(string request, string timeStamp, string nonce)
        {
            Dictionary<string, string> requestHeaders = GetRequestHeaders(timeStamp, nonce);
            Dictionary<string, string> requestBody = GetRequestBody(request);
            Dictionary<string, string> sortedParameters = SortParameters(requestHeaders, requestBody);
            string combinedString = CombineParameters(sortedParameters);

            string signature = SignData(combinedString, private_Key);
            return signature;
        }

        private Dictionary<string, string> GetRequestHeaders(string timeStamp, string nonce)
        {
            return new Dictionary<string, string>
        {
            {"timestamp", timeStamp},
            {"nonce", nonce},
            {"api-key", x_Api_Key},
            {"lang", "en"},
            {"version","1.0" }
        };
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

//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Data;
//using System.Security.Cryptography;
//using System.Text;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using RestSharp;
//using Status_HyperpayFunction.Utils;
//using Status_HyperpayFunction.ViewModels;
//using System.Linq;

//namespace Status_HyperpayFunction
//{
//    public class CardApprovalProcess
//    {
//        //[FunctionName("CardApprovals")]
//        //public void Run([TimerTrigger("*/5 * * * *")] TimerInfo myTimer, ILogger log)
//        //{
//        //    log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
//        //    var lstPendingCards = GetPendingCards(log);
//        //    GetCardStatus(log, lstPendingCards);
//        //}
//        private List<CardsListVm> GetPendingCards(ILogger log)
//        {
//            List<CardsListVm> cardsListVms = new List<CardsListVm>();
//            SqlConnection connection = new SqlConnection(CommonConnection.DBConnection);
//            connection.Open();
//            try
//            {
//                using (SqlCommand sqlCommand = new SqlCommand("select Id,CustomerId,CardTradeNo,AccountHolderStatus,State from Member.CustomerWallet where Type ='Cards' and Provider = 'HyperPay' and State = 'Pending' and CardTradeNo is not null", connection))
//                {
//                    sqlCommand.CommandType = CommandType.Text;
//                    SqlDataAdapter da = new System.Data.SqlClient.SqlDataAdapter(sqlCommand);
//                    DataTable dt = new DataTable();
//                    da.Fill(dt);
//                    foreach (DataRow row in dt.Rows)
//                    {
//                        CardsListVm _cardsListVm = new CardsListVm();

//                        _cardsListVm.Id = Guid.Parse(row["Id"].ToString());
//                        _cardsListVm.CustomerId = Guid.Parse(row["CustomerId"].ToString());
//                        _cardsListVm.CardTradeNo = row["CardTradeNo"].ToString();
//                        _cardsListVm.AccountHolderStatus = row["AccountHolderStatus"].ToString();
//                        _cardsListVm.State = row["State"].ToString();
//                        _cardsListVm.HolderId = row["HolderId"].ToString();
//                        cardsListVms.Add(_cardsListVm);
//                        log.LogInformation("CustomerId: " + _cardsListVm.CustomerId + ", accountHolderStatus" + _cardsListVm.AccountHolderStatus + ", State" + _cardsListVm.State);
//                    }
//                }
//                connection.Close();
//            }
//            catch (Exception ex)
//            {
//                log.LogInformation(ex.Message);
//            }
//            return cardsListVms;
//        }
//        private void GetCardStatus(ILogger log, List<CardsListVm> lstCards)
//        {
//            foreach (var cards in lstCards)
//            {
//                HyperPayCardApplicationResult hyperPayCard = new();
//                log.LogInformation("Entering the Excution query");

//                if ((!string.IsNullOrEmpty(cards.AccountHolderStatus) && cards.AccountHolderStatus.ToLower() != "openingactivated") && cards.State.ToLower() == "pending")
//                {
//                    string query = string.Empty;
//                    string status = string.Empty;

//                    var client = new RestClient(CommonConnection.RestAPIURL + "/api/v1/updatecardstatus/" + cards.CardTradeNo);
//                    var request = new RestRequest(Method.POST);
//                    request.AddParameter("application/json", ParameterType.RequestBody);
//                    IRestResponse response = client.Execute(request);
//                    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
//                    {
//                        hyperPayCard = JsonConvert.DeserializeObject<HyperPayCardApplicationResult>(response.Content);


//                        if (hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.ActivationReviewedSuccess) || hyperPayCard.data.result.card_status == Convert.ToInt32(HyperPayCardApplicationStatusEnum.OpeningActivated))
//                        {
//                            CardApproval cardApproval = new CardApproval();
//                            cardApproval.card_type_id = cards.HolderId;
//                            cardApproval.result = "1";
//                            cardApproval.notify_type = "OPEN_CARD";
//                            cardApproval.mc_trade_no = cards.CardTradeNo;
//                            var obj = JsonConvert.SerializeObject(cardApproval);

//                            var clientCard = new RestClient(CommonConnection.RestAPIURL + "/api/v1/hyperpaycallbackasync");
//                            var requestCard = new RestRequest(Method.POST);
//                            requestCard.AddParameter("application/json", obj, ParameterType.RequestBody);
//                            IRestResponse responseCard = clientCard.Execute(requestCard);

//                        }
//                    }
//                }

//            }

//        }
//        public class CardApproval
//        {
//            public string result { get; set; }
//            public string card_type_id { get; set; }
//            public string notify_type { get; set; }
//            public string mobile_code { get; set; }
//            public string mc_trade_no { get; set; }
//            public string mobile { get; set; }
//            public string remark { get; set; }
//            public string email { get; set; }
//        }

//    }
//}

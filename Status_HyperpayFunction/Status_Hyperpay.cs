using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Status_HyperpayFunction.ViewModels;

namespace Status_HyperpayFunction
{
    public class Status_Hyperpay
    {
        [FunctionName("Function1")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var lstPendingCards = GetPendinCards(log);






            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }


        // need to get data from member.customer table with cards submitted
        // check each card transaction status
        //update the card status in every loop 

        //if Opening – Reviewed - Success   continue  if Opening – Reviewed - Rejected stop that card

        //if Opening – Reviewed - Success call fireblocks
        //after fireblocks okay update the status and call payment 

        // after payment success then call 
        // activate the card

        private List<CardsListVm> GetPendinCards(ILogger log)
        {
            List<CardsListVm> cardsListVms = new List<CardsListVm>();
            SqlConnection connection = new SqlConnection("");
            connection.Open();
            try
            {
                using (SqlCommand sqlCommand = new SqlCommand("select cs.SweepHash,cs.Amount,cs.Datetime,cs.Coin,c.KrakenAsset,c.KrakenMethod,cs.IsGasToken,d.SweepDestinationcomissionValue,d.Id,cs.Coin,d.BatchId from [Exchange].[CryptoSweeping] as cs join Exchange.Deposits as d on cs.DepositRefId = d.id  join master.CoinNetworks as c on cs.CoinNetworkId =c.CoinNetworkId  where cs.IsSweep = 1 and d.SweepStatus = 'Swept' and d.Status!='Approved' and cs.amount is not null", connection))
                {
                    sqlCommand.CommandType = CommandType.Text;
                    SqlDataAdapter da = new System.Data.SqlClient.SqlDataAdapter(sqlCommand);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                    {
                        CardsListVm _cardsListVm = new CardsListVm();

                        //sweepModel.SweepHash = row["SweepHash"].ToString();
                        //sweepModel.Amount = Convert.ToDecimal(row["Amount"].ToString());

                        //if (row["Datetime"] == DBNull.Value)
                        //    sweepModel.SweepDate = null;
                        //else
                        //    sweepModel.SweepDate = Convert.ToDateTime(row["Datetime"].ToString());

                        //sweepModel.IsToken = Convert.ToBoolean(row["IsGasToken"].ToString()) == true ? false : true;
                        //sweepModel.GasAmount = Convert.ToDecimal(row["SweepDestinationcomissionValue"].ToString());



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





    }
}

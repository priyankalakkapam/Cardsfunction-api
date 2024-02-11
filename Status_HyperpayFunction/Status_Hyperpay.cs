using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Status_HyperpayFunction
{
    public class Status_Hyperpay
    {
        [FunctionName("Function1")]
        public void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
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



    }
}

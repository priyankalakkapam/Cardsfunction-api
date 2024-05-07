using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Status_HyperpayFunction.ViewModels
{
    public class HyperPayTxModel
    {
        public string code { get; set; }
        public string msg { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public List<Record> records { get; set; }
        public int total { get; set; }
        public int size { get; set; }
        public int page { get; set; }
    }
    public class Record
    {
        public string tx_id { get; set; }
        public string description { get; set; }
        public string debit { get; set; }
        public string credit { get; set; }
        public string fee { get; set; }
        public int type { get; set; }
        public string tx_currency { get; set; }
        public string tx_amount { get; set; }
        public int status { get; set; }
        public string transaction_date { get; set; }
        public string posting_date { get; set; }
        public string mc_trade_no { get; set; }
        public string card_id { get; set; }
    }

}

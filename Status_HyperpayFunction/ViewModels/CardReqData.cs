using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Status_HyperpayFunction.ViewModels
{
    public class CardReqData
    {
        public string mc_trade_no { get; set; }
    }
    public class HyperPayCardActivateRes
    {
        public string code { get; set; }
        public string msg { get; set; }
    }

    public class HyperPayCardApplicationResult
    {
        public string code { get; set; }
        public string msg { get; set; }
        public HyperPayCardApplicationData data { get; set; }
    }
    public class HyperPayCardApplicationData
    {
        public string mc_trade_no { get; set; }
        public HyperPayCardResult result { get; set; }
    }
    public class HyperPayCardResult
    {
        public string card_id { get; set; }
        public string card_number { get; set; }
        public int card_status { get; set; }
        public string card_type_id { get; set; }
        public long create_timestamp { get; set; }
        public string fail_reason { get; set; }
    }
    public class HyperPayCardActivation
    {
        public string card_id { get; set; }
    }
}

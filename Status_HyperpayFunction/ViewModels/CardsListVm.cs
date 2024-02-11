using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Status_HyperpayFunction.ViewModels
{
    public class CardsListVm
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CardTradeNo { get; set; }
        public string AccountHolderStatus { get; set; }
        public string State { get; set; }
    }
}

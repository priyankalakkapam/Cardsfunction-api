using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Status_HyperpayFunction.ViewModels
{
    public class CompanyConfigration
    {
        public string AddressType { get; set; }
        public string Amount { get; set; }
        public string VaultId { get; set; }
        public string Coin { get; set; }
        public string NetWork { get; set; }
        public string NetworkId { get; set; }
        public string Address { get; set; }
    }
    public class Email
    {
        public bool IsHtml { get; set; }
        public string FromAddress { get; set; }
        public string ReplyAddress { get; set; }
        public string ToAddress { get; set; }
        public string CcAddress { get; set; }
        public string BccAddress { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string AttachmentName { get; set; }
        public byte[] Pdfbytes { get; set; }
        public string AttachmentData { get; set; }
        public string AttachmentContentType { get; set; }
        public string ApiKey { get; set; }
        public string BaseUri { get; set; }
        public string Domain { get; set; }
        public bool Responce { get; internal set; }
    }
}

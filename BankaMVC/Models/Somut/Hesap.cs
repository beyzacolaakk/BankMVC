using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using System.Xml.Serialization;

namespace BankaMVC.Models.Somut
{
    public class Hesap
    {
        [XmlElement("Id")]
        public int Id { get; set; }

        [XmlElement("UserId")]
        public int KullaniciId { get; set; }

        [XmlElement("AccountNumber")]
        public int HesapNo { get; set; }

        [XmlElement("AccountType")]
        public string HesapTipi { get; set; } = string.Empty;

        [XmlElement("Balance")]
        public decimal Bakiye { get; set; }

        [XmlElement("Currency")]
        public string ParaBirimi { get; set; } = "TL";

        [XmlElement("Status")]
        public string? Durum { get; set; }

        [XmlElement("CreatedDate")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    }
}

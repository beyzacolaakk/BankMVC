using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using System.Xml.Serialization;

namespace BankaMVC.Models.Somut
{
    public class Kart 
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("userId")]
        public int KullaniciId { get; set; }

        [XmlElement("cardNumber")]
        public string KartNumarasi { get; set; }

        [XmlElement("cardType")]
        public string KartTipi { get; set; }

        [XmlElement("cvv")]
        public string CVV { get; set; }

        [XmlElement("limit")]
        public decimal? Limit { get; set; }

        [XmlElement("expirationDate")]
        public DateTime SonKullanma { get; set; }

        [XmlElement("status")]
        public string? Durum { get; set; }

        [XmlElement("isActive")]
        public bool Aktif { get; set; }

    }

}

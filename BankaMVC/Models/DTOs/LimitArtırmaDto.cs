using System.Xml.Serialization;

namespace BankaMVC.Models.DTOs
{
    [XmlRoot("LimitIncreaseDto")]
    public class LimitArtırmaDto
    {
        [XmlElement("Id")]
        public int Id { get; set; }
        [XmlElement("UserId")]
        public int KullaniciId { get; set; }
        [XmlElement("FullName")]
        public string AdSoyad { get; set; }


        [XmlElement("CardNumber")]
        public string KartNo { get; set; }

        [XmlElement("CurrentLimit")]
        public decimal? MevcutLimit { get; set; }

        [XmlElement("RequestedLimit")]
        public decimal? TalepEdilenLimit { get; set; }

        [XmlElement("ApplicationDate")]
        public DateTime BasvuruTarihi { get; set; }
        [XmlElement("Status")]
        public string Durum { get; set; }
    }
}

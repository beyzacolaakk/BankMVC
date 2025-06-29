using System.Xml.Serialization;

namespace BankaMVC.Models.DTOs
{
    [XmlRoot("LimitIncreaseCreateDto")]
    public class LimitArtirmaEkleDto 
    {
        [XmlElement("currentLimit")]
        public decimal MevcutLimit { get; set; }
        [XmlElement("requestedLimit")]
        public decimal TalepEdilenLimit { get; set; }
        [XmlElement("cardNumber")]
        public string KartNo { get; set; }
        [XmlElement("status")]
        public string? Durum {  get; set; }
        [XmlElement("id")]
        public int Id { get; set; }

        public int KartId { get; set; } 
    }
}

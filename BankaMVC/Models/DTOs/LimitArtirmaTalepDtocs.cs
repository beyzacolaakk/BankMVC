using System.Xml.Serialization;

namespace BankaMVC.Models.DTOs
{
    [XmlRoot("LimitIncreaseRequestDto")]
    public class LimitIncreaseRequestDto 
    {
        [XmlElement("cardId")]
        public int CardId { get; set; }

        [XmlElement("currentLimit")]
        public decimal CurrentLimit { get; set; }

        [XmlElement("newLimit")]
        public decimal NewLimit { get; set; }
    }
}

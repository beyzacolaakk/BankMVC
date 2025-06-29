using System.Xml.Serialization;

namespace BankaMVC.Models.DTOs
{
    [XmlRoot("SupportRequest")]
    public class SupportRequestUpdateDto
    {
        [XmlElement("Id")]
        public int Id { get; set; }
        [XmlElement("Status")]
        public string? Status { get; set; }
        [XmlElement("Response")]
        public string? Response { get; set; }
    }
}

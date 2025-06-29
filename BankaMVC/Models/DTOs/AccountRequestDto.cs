using System.Xml.Serialization;

namespace BankaMVC.Models.DTOs
{
    [XmlRoot("AccountRequestDto")]
    public class AccountRequestDto
    {
        [XmlElement("Id")]
        public int Id { get; set; }

        [XmlElement("FullName")]
        public string FullName { get; set; } = string.Empty;

        [XmlElement("AccountNumber")]
        public string AccountNumber { get; set; } = string.Empty;

        [XmlElement("ApplicationDate")]
        public DateTime ApplicationDate { get; set; }

        [XmlElement("Status")]
        public string Status { get; set; } = string.Empty;

        [XmlElement("PhoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [XmlElement("Email")]
        public string Email { get; set; } = string.Empty;
    }
}

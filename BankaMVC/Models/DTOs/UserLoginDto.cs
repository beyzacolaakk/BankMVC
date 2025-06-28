using System.Xml.Serialization;

namespace BankaMVC.Models.DTOs
{
    [XmlRoot("UserLoginDto")]
    public class UserLoginDto
    {
        [XmlElement("phone")]
        public string Phone { get; set; }

        [XmlElement("password")]
        public string Password { get; set; }

        [XmlElement("ipAddress")]
        public string IpAddress { get; set; }
    }
}

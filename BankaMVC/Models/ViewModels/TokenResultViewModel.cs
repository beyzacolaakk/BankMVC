using System.Xml.Serialization;

namespace BankaMVC.Models.ViewModels
{
    [XmlRoot("AccessToken")]
    public class TokenResultViewModel
    {
        [XmlElement("Token")]
        public string Token { get; set; }

        [XmlElement("Expiration")]
        public DateTime Expiration { get; set; }
    }
}

using System.Xml.Serialization;

namespace BankaMVC.Models.DTOs
{
    public class KullaniciBilgileriDto
    {
        [XmlElement("fullName")]
        public string AdSoyad { get; set; }

        [XmlElement("email")]
        public string Email { get; set; }

        [XmlElement("phone")]
        public string Telefon { get; set; }

        [XmlElement("branch")]
        public string Sube { get; set; }
    }

}

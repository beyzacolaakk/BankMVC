using System.Xml.Serialization;

namespace BankaMVC.Models.DTOs
{
    [XmlRoot("UpdateStatusDto")]
    public class DurumuGuncelleDto
    {
        [XmlElement("id")]
        public int? Id { get; set; }
        [XmlElement("status")]
        public string? Durum { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Banka.Varlıklar.DTOs
{
    [XmlRoot("SupportRequestDto")]
    public class DestekTalebiOlusturDto
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("userId")]
        public int KullaniciId { get; set; }

        [XmlElement("subject")]
        public string Konu { get; set; }

        [XmlElement("message")]
        public string Mesaj { get; set; }

        [XmlElement("status")]
        public string? Durum { get; set; }
        [XmlElement("response")]
        public string? Yanit { get; set; }

        [XmlElement("category")]
        public string? Kategori { get; set; }

        [XmlElement("fullName")]
        public string? AdSoyad { get; set; }

        [XmlElement("date")]
        public DateTime? Tarih { get; set; }


    }
}

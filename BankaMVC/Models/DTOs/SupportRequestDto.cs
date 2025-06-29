using System.Xml.Serialization;
using System;
using System.Xml.Serialization;
namespace BankaMVC.Models.DTOs
{


    [XmlRoot("SupportRequestDto")]
    public class SupportRequestDto
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("userId")]
        public int UserId { get; set; }

        [XmlElement("subject")]
        public string Subject { get; set; } = string.Empty;

        [XmlElement("message")]
        public string Message { get; set; } = string.Empty;

        [XmlElement("status", IsNullable = true)]
        public string? Status { get; set; }

        [XmlElement("response", IsNullable = true)]
        public string? Response { get; set; }

        [XmlElement("category", IsNullable = true)]
        public string? Category { get; set; }

        [XmlElement("fullName", IsNullable = true)]
        public string? FullName { get; set; }

        [XmlElement("date", IsNullable = true)]
        public DateTime? Date { get; set; }
    }

}

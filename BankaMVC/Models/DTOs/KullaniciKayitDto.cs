
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Banka.Varlıklar.DTOs
{

    public class UserRegisterDto
    {
        [XmlElement("fullName")]
        public string FullName { get; set; } = string.Empty;

        [XmlElement("email")]
        public string Email { get; set; } = string.Empty;

        [XmlElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [XmlElement("password")]
        public string Password { get; set; } = string.Empty;

        [XmlElement("branch")]
        public int Branch { get; set; }

        [XmlElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}

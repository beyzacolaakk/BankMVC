using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using System.Xml.Serialization;

namespace BankaMVC.Models.Somut
{
    [XmlRoot("BranchDto")]
    public class SubeDto
    {
        [XmlElement("Id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Şube adı boş olamaz.")]
        [StringLength(100, ErrorMessage = "Şube adı en fazla 100 karakter olabilir.")]
        [XmlElement("BranchName")]  // XML'deki BranchName elementine denk gelsin
        public string SubeAdi { get; set; } = string.Empty;
    }
}

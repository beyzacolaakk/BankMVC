using System.ComponentModel.DataAnnotations;

namespace BankaMVC.Models.ViewModels
{
    public class GirisViewModel
    {
        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [Display(Name = "Phone")]
        public string Telefon { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        [Display(Name = "Password")]
        public string Sifre { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool BeniHatirla { get; set; }
    }
}

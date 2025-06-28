
using BankaMVC.Models.Somut;
using System.ComponentModel.DataAnnotations;

namespace BankaMVC.Models.ViewModels
{
    public class ParaTransferiViewModel
    {
        [Required(ErrorMessage = "Ödeme aracı seçimi zorunludur")]
        public string OdemeAraci { get; set; } = "account"; // "hesap" veya "kart"

        public string? SecilenHesapId { get; set; }
        public string? SecilenKartId { get; set; }

        [Required(ErrorMessage = "Alıcı hesap numarası zorunludur")]
        [Display(Name = "Recipient Account Number")]
        public string AliciHesapNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Transfer tutarı zorunludur")]
        [Range(1, double.MaxValue, ErrorMessage = "Transfer tutarı 1 TL'den büyük olmalıdır")]
        [Display(Name = "Transfer Amount")]
        public decimal Tutar { get; set; }

        [Required(ErrorMessage = "İşlem tarihi zorunludur")]
        [Display(Name = "Transaction Date")]
        public DateTime IslemTarihi { get; set; } = DateTime.Today;

        [Display(Name = "Explanation")]
        [MaxLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir")]
        public string? Aciklama { get; set; }

        // View için gerekli listeler
        public List<Hesap> Hesaplar { get; set; } = new List<Hesap>();
        public List<Kart> Kartlar { get; set; } = new List<Kart>();
    }
}

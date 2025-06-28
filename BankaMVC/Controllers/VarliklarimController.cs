
using BankaMVC.Filters;
using BankaMVC.Models.DTOs;
using BankaMVC.Models.Result;
using BankaMVC.Models.Somut;
using BankaMVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Xml.Linq;

namespace BankaMVC.Controllers
{
    public class VarliklarimController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public VarliklarimController(IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
        }
        private readonly DovizKuru _dovizKuru = new DovizKuru();
        [RoleAuthorize("Customer")]
        public async Task<IActionResult> Index(string paraBirimi, string bolum = "")
        {
            if (string.IsNullOrEmpty(paraBirimi))
            {
                paraBirimi = "TRY"; 
            }
            var sonuc = await VarliklarimiGetirAsync();
            KartBorcHesapla(sonuc);

            var dovizKuru = new DovizKuru(); // buradaki oranlar elle girilmiş ya da başka bir yerden alınıyor olmalı

            var toplamPara = ConvertCurrency(sonuc.ToplamPara, "TRY", paraBirimi, dovizKuru);
            var toplamBorc = ConvertCurrency(sonuc.ToplamBorc, "TRY", paraBirimi, dovizKuru);
            foreach (var kart in sonuc.Kartlar)
            {
                kart.KartBorc = ConvertCurrency(kart.KartBorc, "TRY", paraBirimi, dovizKuru);
            }
            foreach (var hesap in sonuc.Hesaplar) 
            {
                hesap.Bakiye = ConvertCurrency(hesap.Bakiye, "TRY", paraBirimi, dovizKuru);
            }
            var viewModel = new VarliklarimViewModel
            {
                Hesaplar = sonuc.Hesaplar,
                Kartlar = sonuc.Kartlar,
                SecilenParaBirimi = paraBirimi,
                AktifBolum = bolum,
                ToplamVarlik = toplamPara,
                ToplamBorc = toplamBorc,
                NetVarlik = toplamPara - toplamBorc
            };

            return View(viewModel);
        }


        private decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency, DovizKuru kur)
        {
            if (fromCurrency == toCurrency) return amount;

            return (fromCurrency, toCurrency) switch
            {
                ("TRY", "USD") => amount * kur.TRY_USD,
                ("TRY", "EUR") => amount * kur.TRY_EUR,
                ("USD", "TRY") => amount * kur.USD_TRY,
                ("USD", "EUR") => amount * kur.USD_EUR,
                ("EUR", "TRY") => amount * kur.EUR_TRY,
                ("EUR", "USD") => amount * kur.EUR_USD,
                _ => amount
            };
        }
        private void KartBorcHesapla(VarliklarViewModel sonuc)
        {
            foreach (var kart in sonuc.Kartlar)
            {
                kart.KartBorc = 5000 - kart.Limit;
            }
        }
        private async Task<VarliklarViewModel> VarliklarimiGetirAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];
                if (string.IsNullOrEmpty(token))
                    return new VarliklarViewModel();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Account/getassets";
                var response = await client.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                    return new VarliklarViewModel();

                var xml = await response.Content.ReadAsStringAsync();
                var doc = XDocument.Parse(xml);

                var dataElement = doc.Root?.Element("Data");
                if (dataElement == null)
                    return new VarliklarViewModel();

                var model = new VarliklarViewModel
                {
                    ToplamPara = decimal.Parse(dataElement.Element("totalBalance")?.Value ?? "0"),
                    ToplamBorc = decimal.Parse(dataElement.Element("totalDebt")?.Value ?? "0"),

                    Hesaplar = dataElement.Element("accounts")?
                        .Elements("account")
                        .Select(a => new HesapDto
                        {
                            HesapTipi = a.Element("AccountType")?.Value ?? "",
                            ParaBirimi = a.Element("Currency")?.Value ?? "",
                            Bakiye = decimal.Parse(a.Element("Balance")?.Value ?? "0")
                        }).ToList() ?? new List<HesapDto>(),

                    Kartlar = dataElement.Element("cards")?
                        .Elements("card")
                        .Select(k => new KartDto
                        {
                            KartNumarasi = k.Element("KartNumarasi")?.Value ?? "",
                            Limit = decimal.Parse(k.Element("Limit")?.Value ?? "0"),
                            KartBorc = decimal.Parse(k.Element("KartBorc")?.Value ?? "0")
                        }).ToList() ?? new List<KartDto>()
                };

                return model;
            }
        }




    }
}

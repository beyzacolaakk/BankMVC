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
    [ServiceFilter(typeof(TokenCheckFilter))]
    public class GostergePaneliController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public GostergePaneliController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        [RoleAuthorize("Customer")]
        public async Task<IActionResult> Index()
        {
            var model = new GostergePaneliViewModel();
            model.Kartlar = await KartlariGetirAsync();
            model.Hesaplar = await HesaplariGetirAsync();
            model.Kullanici = await KullaniciBilgileriGetirAsync();
            return View(model);
        }
        public async Task<IActionResult> Cikis()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = Request.Cookies["UserJwtToken"];

                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Zaten çıkış yapmışsınız.";
                    return RedirectToAction("Index", "Giris");
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var apiUrl = StaticSettings.ApiBaseUrl + "v2/Auth/logout";

                var response = await client.PostAsync(apiUrl, null);

                if (response.IsSuccessStatusCode)
                {
           
                    Response.Cookies.Delete("UserJwtToken");
                    TempData["Success"] = "Çıkış yapıldı.";
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Hata: " + errorContent);
                    TempData["Error"] = "Çıkış işlemi başarısız oldu.";
                }

                return RedirectToAction("Index", "Giris");
            }
        }
        private async Task<List<Kart>> KartlariGetirAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];
                if (string.IsNullOrEmpty(token)) return new List<Kart>();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Card/getallbyuserid";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();
                    var doc = XDocument.Parse(xml);

                    // İç veri listesini çek
                    var cards = doc.Descendants("Card")
                                   .Select(x => new Kart
                                   {
                                       Id = int.Parse(x.Element("id")?.Value ?? "0"),
                                       KullaniciId = int.Parse(x.Element("userId")?.Value ?? "0"),
                                       KartNumarasi = x.Element("cardNumber")?.Value,
                                       KartTipi = x.Element("cardType")?.Value,
                                       CVV = x.Element("cvv")?.Value,
                                       Limit = decimal.TryParse(x.Element("limit")?.Value, out var lim) ? lim : 0,
                                       SonKullanma = DateTime.TryParse(x.Element("expirationDate")?.Value, out var dt) ? dt : DateTime.MinValue,
                                       Durum = x.Element("status")?.Value,
                                       Aktif = bool.TryParse(x.Element("isActive")?.Value, out var aktif) && aktif
                                   })
                                   .ToList();

                    return cards;

                }

                return new List<Kart>();
            }
        }
        private async Task<List<Hesap>> HesaplariGetirAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];
                if (string.IsNullOrEmpty(token)) return new List<Hesap>();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Account/getallbyuserid";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();
                    var doc = XDocument.Parse(xml);

                    var success = bool.Parse(doc.Root.Element("Success")?.Value ?? "false");
                    var message = doc.Root.Element("Message")?.Value ?? "";

                    var hesaplar = doc.Root.Element("Data")?.Elements("Account")
                        .Select(x => new Hesap
                        {
                            Id = int.Parse(x.Element("Id")?.Value ?? "0"),
                            KullaniciId = int.Parse(x.Element("UserId")?.Value ?? "0"),
                            HesapNo = int.Parse(x.Element("AccountNumber")?.Value ?? "0"),
                            HesapTipi = x.Element("AccountType")?.Value ?? "",
                            Bakiye = decimal.Parse(x.Element("Balance")?.Value ?? "0"),
                            ParaBirimi = x.Element("Currency")?.Value ?? "",
                            Durum = x.Element("Status")?.Value ?? "",
                            OlusturmaTarihi = DateTime.TryParse(x.Element("CreatedDate")?.Value, out var date) ? date : DateTime.Now
                        }).ToList();

                    return hesaplar ?? new List<Hesap>();
                }


                return new List<Hesap>();
            }
        }
        private async Task<KullaniciBilgileriDto> KullaniciBilgileriGetirAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];
                if (string.IsNullOrEmpty(token)) return new KullaniciBilgileriDto();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "User/getuser";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();
                    var doc = XDocument.Parse(xml);

                    var dataElement = doc.Root?.Element("Data");

                    if (dataElement != null)
                    {
                        var dto = new KullaniciBilgileriDto
                        {
                            AdSoyad = dataElement.Element("fullName")?.Value ?? "",
                            Email = dataElement.Element("email")?.Value ?? "",
                            Telefon = dataElement.Element("phone")?.Value ?? "",
                            Sube = dataElement.Element("branch")?.Value ?? ""
                        };

                        return dto;
                    }

                    return new KullaniciBilgileriDto(); 
                }
                else
                {
                    return new KullaniciBilgileriDto();
                }

            }
        }



    }
}

using BankaMVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankaMVC.Controllers
{
    using Banka.Varlıklar.DTOs;
    using BankaMVC.Filters;
    using BankaMVC.Models.DTOs;
    using BankaMVC.Models.Result;
    using BankaMVC.Models.Somut;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Xml.Linq;

    namespace BankingMVC.Controllers
    {
        public class ParaIslemController : Controller
        {
            private readonly IHttpContextAccessor _httpContextAccessor;
            public ParaIslemController(IHttpContextAccessor httpContextAccessor) 
            {
                _httpContextAccessor = httpContextAccessor;
            }

            [RoleAuthorize("Customer")]
            public async Task<IActionResult> Index()
            {
                var model = new ParaIslemViewModel
                {
                    Hesaplar =await HesaplariGetirAsync(),
                    Kartlar = await KartlariGetirAsync(),
                };
                return View(model);
            }

        
            [HttpPost]
            public async Task<IActionResult> ParaCekYatir(ParaIslemViewModel paraIslemViewModel)
            {
                if (!ModelState.IsValid)
                {
                    return View("Basarisiz",paraIslemViewModel);
                }
                ParaCekYatirDto paraCekYatirDto=new ParaCekYatirDto();
                if (paraIslemViewModel.AracTuru =="card")
                {
                    paraCekYatirDto = new ParaCekYatirDto
                    {

                        Tutar = paraIslemViewModel.Tutar ?? paraIslemViewModel.YTutar ?? 0,
                        IslemTipi = paraIslemViewModel.IslemTuru,
                        HesapId =  paraIslemViewModel.SecilenKartId,
                        Aciklama = "",
                  IslemTuru=paraIslemViewModel.AracTuru,


                };
                }
                else
                {
                    paraCekYatirDto = new ParaCekYatirDto
                    {

                        Tutar = paraIslemViewModel.Tutar ?? paraIslemViewModel.YTutar ?? 0,
                        IslemTipi = paraIslemViewModel.IslemTuru,
                        HesapId = paraIslemViewModel.SecilenHesapId!.Value.ToString(),
                        Aciklama = "",
                     IslemTuru= paraIslemViewModel.AracTuru,   
                

                };
                }
            
            
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using (var client = new HttpClient(handler))
                {
                    var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];

                    if (string.IsNullOrEmpty(token))
                    {
                        TempData["Error"] = "Giriş yapmanız gerekiyor.";
                        return RedirectToAction("Giris", "Hesap");
                    }

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var apiUrl = StaticSettings.ApiBaseUrl + "Islem/paracekyatir";

                    var content = new StringContent(JsonConvert.SerializeObject(paraCekYatirDto), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, content);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Success"] = "Talebiniz başarıyla oluşturuldu.";
                        return View("Basarili", paraIslemViewModel);
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Hata Kodu: " + response.StatusCode);
                        Console.WriteLine("Hata Açıklaması: " + response.ReasonPhrase);
                        Console.WriteLine("Hata İçeriği: " + errorContent);
                        TempData["Error"] = "Talep oluşturulurken bir hata oluştu.";
                        return View("Basarisiz",paraIslemViewModel);
                    }
                }
            }
            public IActionResult Basarili()
            {
                if (TempData["IslemTuru"] == null)
                {
                    return RedirectToAction("Index");
                }

                ViewBag.IslemTuru = TempData["IslemTuru"];
                ViewBag.Tutar = TempData["Tutar"];
                ViewBag.IslemNo = TempData["IslemNo"];

                return View();
            }
            [HttpGet]
            private async Task<List<Kart>> KartlariGetirAsync()
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using (var client = new HttpClient(handler))
                {
                    var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];

                    if (string.IsNullOrEmpty(token))
                    {
                        return new List<Kart>(); // Token yoksa boş döner
                    }

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                    var apiUrl = StaticSettings.ApiBaseUrl + "Card/getallbyuserid";

                    var response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var xml = await response.Content.ReadAsStringAsync();
                        var kartlar = ParseKartlarWithXDocument(xml);
                        return kartlar;
                    }


                    return new List<Kart>();
                }
            }
            private List<Kart> ParseKartlarWithXDocument(string xml)
            {
                var kartlar = new List<Kart>();
                var doc = XDocument.Parse(xml);

                var cards = doc.Descendants("Card");

                foreach (var card in cards)
                {
                    var kart = new Kart
                    {
                        Id = (int?)card.Element("id") ?? 0,
                        KullaniciId = (int?)card.Element("userId") ?? 0,
                        KartNumarasi = (string?)card.Element("cardNumber") ?? "",
                        KartTipi = (string?)card.Element("cardType") ?? "",
                        CVV = (string?)card.Element("cvv") ?? "",
                        Limit = decimal.TryParse(card.Element("limit")?.Value, out var limit) ? limit : (decimal?)null,

                        SonKullanma = DateTime.TryParse((string?)card.Element("expirationDate"), out var date) ? date : DateTime.MinValue,
                        Durum = (string?)card.Element("status") ?? "",
                        Aktif = (bool?)card.Element("isActive") ?? false
                    };

                    kartlar.Add(kart);
                }

                return kartlar;
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

                    if (string.IsNullOrEmpty(token))
                    {
                        return new List<Hesap>();
                    }

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                    var apiUrl = StaticSettings.ApiBaseUrl + "Account/getallbyuserid";

                    var response = await client.GetAsync(apiUrl);
                    if (!response.IsSuccessStatusCode)
                        return new List<Hesap>();

                    var xmlContent = await response.Content.ReadAsStringAsync();

                    var xdoc = XDocument.Parse(xmlContent);
                    var hesaplar = xdoc.Descendants("Account")
                        .Select(x => new Hesap
                        {
                            Id = (int)x.Element("Id"),
                            KullaniciId = (int)x.Element("UserId"),
                            HesapNo = int.Parse((string)x.Element("AccountNumber")),
                            HesapTipi = (string)x.Element("AccountType") ?? string.Empty,
                            Bakiye = decimal.Parse((string)x.Element("Balance") ?? "0"),
                            ParaBirimi = (string)x.Element("Currency") ?? "TL",
                            Durum = (string?)x.Element("Status"),
                            OlusturmaTarihi = DateTime.Parse((string)x.Element("CreatedDate"))
                        }).ToList();

                    return hesaplar;
                }
            }
        }
    }
}

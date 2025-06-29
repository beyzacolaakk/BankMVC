using Microsoft.AspNetCore.Mvc;

namespace BankaMVC.Controllers
{
    using Banka.Varlıklar.DTOs;
    using BankaMVC.Filters;
    using BankaMVC.Models.DTOs;
    using BankaMVC.Models.Result;
    using BankaMVC.Models.Somut;
    using BankaMVC.Models.ViewModels;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Xml.Linq;

    namespace ParaTransfer.Controllers
    {
        public class ParaTransferiController : Controller
        {
            private readonly IHttpContextAccessor _httpContextAccessor;
            public ParaTransferiController(IHttpContextAccessor httpContextAccessor) 
            {
                _httpContextAccessor = httpContextAccessor;
            }
            [RoleAuthorize("Customer")]
            [HttpGet]
            public async Task<IActionResult> Index()
            {
                var model = new ParaTransferiViewModel
                {
                    Hesaplar = await HesaplariGetirAsync(),
                    Kartlar = await KartlariGetirAsync(),
                    IslemTarihi = DateTime.Today
                };

                return View(model);
            }
            [RoleAuthorize("Customer")]
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Index(ParaTransferiViewModel model)
            {
                
                if (model.OdemeAraci == "account" && string.IsNullOrEmpty(model.SecilenHesapId))
                {
                    ModelState.AddModelError("SecilenHesapId", "Lütfen bir hesap seçiniz");
                }
                else if (model.OdemeAraci == "card" && string.IsNullOrEmpty(model.SecilenKartId))
                {
                    ModelState.AddModelError("SecilenKartId", "Lütfen bir kredi kartı seçiniz");
                }

               
                if (model.IslemTarihi < DateTime.Today)
                {
                    ModelState.AddModelError("IslemTarihi", "İşlem tarihi bugünden önce olamaz");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                     
                        var sonuc = TransferIslemiGerceklestir(model);

                        if (sonuc)
                        {
                            TempData["SuccessMessage"] = "Transfer işlemi başarıyla gerçekleştirildi!";
                            return RedirectToAction("Basarili");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Transfer işlemi sırasında bir hata oluştu");
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Transfer işlemi sırasında bir hata oluştu: " + ex.Message);
                    }
                }

                model.Hesaplar = await HesaplariGetirAsync();
                model.Kartlar = await KartlariGetirAsync();

                return View(model);
            }

            [HttpGet]
            public IActionResult Basarili()
            {
                return View();
            }

       

            private bool TransferIslemiGerceklestir(ParaTransferiViewModel model)
            {
        
                System.Threading.Thread.Sleep(1000);
                return true;
            }
            [HttpPost]
            public async Task<IActionResult> ParaGonderme(ParaTransferiViewModel paraTransferiViewModel)
            {
                if (!ModelState.IsValid)
                {
                    return View("Basarisiz", paraTransferiViewModel);
                }

                MoneyTransferDto paraGonderme = new MoneyTransferDto
                {
                    Description= paraTransferiViewModel.Aciklama!,
                    ReceiverAccountId = paraTransferiViewModel.AliciHesapNo,
                    SenderAccountId = paraTransferiViewModel.OdemeAraci == "card"
                                        ? paraTransferiViewModel.SecilenKartId!
                                        : paraTransferiViewModel.SecilenHesapId!,
                    TransactionType= "Money Transfer",
                    Amount = paraTransferiViewModel.Tutar,
                    PaymentMethod = paraTransferiViewModel.OdemeAraci
                };

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
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                    // XML içeriğe çevirme
                    var xml = XmlConverter.XmlConverter.SerializeToXml(paraGonderme);
                    var content = new StringContent(xml, Encoding.UTF8, "application/xml");

                    var apiUrl = StaticSettings.ApiBaseUrl + "Transaction/sendmoney";
                    var response = await client.PostAsync(apiUrl, content);

                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Success"] = "Talebiniz başarıyla oluşturuldu.";
                        return View("Basarili", paraTransferiViewModel);
                    }
                    else
                    {
                        Console.WriteLine("Hata Kodu: " + response.StatusCode);
                        Console.WriteLine("Hata Açıklaması: " + response.ReasonPhrase);
                        Console.WriteLine("Hata İçeriği: " + responseContent);
                        TempData["Error"] = "Talep oluşturulurken bir hata oluştu.";
                        return View("Basarisiz", paraTransferiViewModel);
                    }
                }
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
                        return new List<Kart>();
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

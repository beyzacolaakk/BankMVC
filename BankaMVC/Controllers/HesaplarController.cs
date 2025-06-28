
using Banka.Varlıklar.DTOs;
using BankaMVC.Filters;
using BankaMVC.Models.Result;
using BankaMVC.Models.Somut;
using BankaMVC.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace BankaMVC.Controllers
{
    public class HesaplarController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public HesaplarController(IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
        }
        [RoleAuthorize("Customer")]
        public async Task<IActionResult> Index(string sekme = "tum")
        {
            var hesaplar = new HesaplarViewModel();
            hesaplar.Hesaplar = await HesaplariGetirAsync();

            IEnumerable<Hesap> filtrelenmisHesaplar = hesaplar.Hesaplar;

            if (sekme == "vadesiz")
                filtrelenmisHesaplar = hesaplar.Hesaplar.Where(h => h.HesapTipi == "Demand Deposit");
            else if (sekme == "vadeli")
                filtrelenmisHesaplar = hesaplar.Hesaplar.Where(h => h.HesapTipi == "Term");

            var viewModel = new HesaplarViewModel
            {
                Hesaplar = filtrelenmisHesaplar.ToList(),
                AktifSekme = sekme
            };

            return View(viewModel);
        }
        [AllowAnonymous] 
        public IActionResult Yetkisiz()
        {
            return View();
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



        [HttpGet]
        public IActionResult YeniHesap()
        {
            var model = new YeniHesapViewModel
            {
                HesapTipleri = new List<HesapTipiOption>
                {
                    new HesapTipiOption
                    {
                        Value = "Demand Deposit",
                      Text = "Demand Deposit Account",
 Description = "Ideal for daily transactions, withdraw money anytime",
            Features = new List<string> { "24/7 Withdrawals", "Online Transactions", "Shopping with Card" }
                    },
                    new HesapTipiOption
                    {
                        Value = "Term",
                            Text = "Term Deposit Account",
              Description = "Grow your money with high interest rates",
            Features = new List<string> { "High Interest", "Fixed Return", "Flexible Terms" }
                    }
                },
                ParaBirimleri = new List<ParaBirimiOption>
                {
                    new ParaBirimiOption
                    {
                        Value = "TL",
                        Text = "Türk Lirası",
                        Symbol = "₺",
                    Description = "For all your transactions in Turkey"
                    },
                    new ParaBirimiOption
                    {
                        Value = "USD",
                        Text = "Amerikan Doları",
                        Symbol = "$",
           Description = "For international transactions"
                    },
                    new ParaBirimiOption
                    {
                        Value = "EUR",
                        Text = "Euro",
                        Symbol = "€",
                  Description = "For European transactions and currency diversity"
                    }
                }
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> YeniHesapOlustur(YeniHesapViewModel yeniHesapViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View("Basarisiz", yeniHesapViewModel);
            }

            var hesapOlusturDto = new CreateAccountDto
            {
                AccountType = yeniHesapViewModel.HesapTipi,
                Currency= yeniHesapViewModel.ParaBirimi,
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
                    TempData["Error"] = "You need to log in.";
                    return RedirectToAction("Giris", "Hesap");
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var xml = XmlConverter.XmlConverter.SerializeToXml(hesapOlusturDto);
                var content = new StringContent(xml, Encoding.UTF8, "application/xml");

                var apiUrl = StaticSettings.ApiBaseUrl + "Account/createautomaticaccount";
                var response = await client.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Your request has been successfully created.";
                    return View("Basarili", yeniHesapViewModel);
                }
                else
                {
                    Console.WriteLine("Status Code: " + response.StatusCode);
                    Console.WriteLine("Reason: " + response.ReasonPhrase);
                    Console.WriteLine("Error Content: " + responseContent);

                    TempData["Error"] = "An error occurred while creating the request.";
                    return View("Basarisiz", yeniHesapViewModel);
                }
            }
        }

    }

}

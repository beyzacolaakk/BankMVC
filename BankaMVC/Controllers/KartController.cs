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
using System.Xml;
using System.Xml.Linq;

namespace BankaMVC.Controllers
{
    public class KartController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public KartController(IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
        }
        [RoleAuthorize("Customer")]
        public async Task<IActionResult> Index(string sekme = "tum")
        {
            var kartlar = new KartlarViewModel();
            kartlar.Kartlar = await KartlariGetirAsync();

            IEnumerable<Kart> filtrelenmisKartlar = kartlar.Kartlar; 

            if (sekme == "banka")
                filtrelenmisKartlar = kartlar.Kartlar.Where(h => h.KartTipi == "Bank"); 
            else if (sekme == "kredi")
                filtrelenmisKartlar = kartlar.Kartlar.Where(h => h.KartTipi == "Credit");

            var viewModel = new KartlarViewModel
            {
                Kartlar = filtrelenmisKartlar.ToList(),
                AktifSekme = sekme
            };

            return View(viewModel);
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
                    Limit = (decimal?)card.Element("limit") ?? 0,
                    SonKullanma = DateTime.TryParse((string?)card.Element("expirationDate"), out var date) ? date : DateTime.MinValue,
                    Durum = (string?)card.Element("status") ?? "",
                    Aktif = (bool?)card.Element("isActive") ?? false
                };

                kartlar.Add(kart);
            }

            return kartlar;
        }



        [HttpPost]
        public async Task<IActionResult> YeniKartOlustur(YeniKartViewModel yeniKartViewModel)
        {
            if (!ModelState.IsValid)
                return View("Basarisiz");

            var dto = new CreateCardDto
            {
                CardType = yeniKartViewModel.KartTipi,
            };

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);

            var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "You must be logged in.";
                return RedirectToAction("Giris", "Hesap");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            var xml = XmlConverter.XmlConverter.SerializeToXml(dto);
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");

            var response = await client.PostAsync(StaticSettings.ApiBaseUrl + "Card/createautomaticcard", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Your request has been submitted successfully.";
                return View("Basarili", dto);
            }

            TempData["Error"] = "An error occurred while creating the request.";
            return View("Basarisiz", dto);
        }

        [HttpPost]
        public async Task<IActionResult> LimitAritrmaEkle(int KartId, decimal MevcutLimit, int YeniLimit)
        {
            var veri = new LimitIncreaseRequestDto
            {
                CardId = KartId,
                CurrentLimit = MevcutLimit,
                NewLimit = YeniLimit
            };

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);

            var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "You must be logged in.";
                return RedirectToAction("Giris", "Hesap");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            var xml = XmlConverter.XmlConverter.SerializeToXml(veri);
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");

            var response = await client.PostAsync(StaticSettings.ApiBaseUrl + "LimitIncrease/addlimitincrease", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Your request has been submitted successfully.";
                return View("Basarili", veri);
            }

            TempData["Error"] = "An error occurred while submitting your request.";
            return View("Basarisiz", veri);
        }

        [HttpGet]
        public IActionResult YeniKart() 
        {

            var model = new YeniKartViewModel(); 
            return View(model);
        }
     

    }
}

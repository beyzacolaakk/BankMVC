using Banka.Varlıklar.DTOs;
using BankaMVC.Filters;
using BankaMVC.Models.DTOs;
using BankaMVC.Models.Result;
using BankaMVC.Models.Somut;
using BankaMVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;

namespace BankaMVC.Controllers
{
    public class DestekController : Controller
    {    
        private readonly IHttpContextAccessor _httpContextAccessor;
        public DestekController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        [RoleAuthorize("Customer")]
        public async Task<IActionResult> Index(string durum = "all", string arama = "")
        {
            var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];

            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Giriş yapmanız gerekiyor.";
                return RedirectToAction("Giris", "Hesap");
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = $"{StaticSettings.ApiBaseUrl}SupportRequest/filter?status={durum}&search={arama}";

                var response = await client.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Destek talepleri alınamadı.";
                    return View(new DestekViewModel());
                }

                var xml = await response.Content.ReadAsStringAsync();
                var xdoc = XDocument.Parse(xml);

                var talepler = xdoc.Descendants("SupportRequest")
        .Select(x => new DestekTalebi
        {
            Id = (int?)x.Element("id") ?? 0,
            KullaniciId = (int?)x.Element("userId") ?? 0,
            Konu = x.Element("subject")?.Value ?? "",
            Mesaj = x.Element("message")?.Value ?? "",
            Durum = x.Element("status")?.Value ?? "",
            Kategori = x.Element("category")?.Value ?? "",
            Yanit = x.Element("response")?.Value,
            OlusturmaTarihi = DateTime.TryParse(x.Element("createdDate")?.Value, out var tarih) ? tarih : DateTime.MinValue
        }).ToList();


                var viewModel = new DestekViewModel
                {
                    Talepler = talepler.OrderByDescending(t => t.OlusturmaTarihi).ToList(),
                    DurumFiltre = durum,
                    AramaMetni = arama,
                };

                return View(viewModel);
            }
        }







        public IActionResult YeniTalep()
        {
            return View(new DestekTalebi());
        }




        [HttpPost]
        public async Task<IActionResult> YeniTalep(DestekTalebiOlusturDto model, string Kategori)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
          
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];
                var res= new SupportRequestDto{ 
                Category = Kategori,
                Subject = model.Konu,   
                FullName=model.AdSoyad,
                Message = model.Mesaj,
                Response=model.Yanit,
                Status=model.Durum,
                Date=model.Tarih,
                UserId=model.KullaniciId
                
                };
                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Giriş yapmanız gerekiyor.";
                    return RedirectToAction("Giris", "Hesap");
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

          
                var xml = XmlConverter.XmlConverter.SerializeToXml(res); 
                var content = new StringContent(xml, Encoding.UTF8, "application/xml");

                var apiUrl = StaticSettings.ApiBaseUrl + "SupportRequest/createsupportrequest";

                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Talebiniz başarıyla oluşturuldu.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "Talep oluşturulurken bir hata oluştu.";
                    return View(model);
                }
            }
        }

    }
}

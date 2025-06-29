using BankaMVC.Models.DTOs;
using BankaMVC.Models.Result;
using BankaMVC.Models.Somut.BankaYonetimPaneli.Models;
using BankaMVC.Models.Somut;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Collections.Generic;
using BankaMVC.Filters;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace BankaMVC.Controllers
{
    public class AdminPanelController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AdminPanelController(IHttpContextAccessor httpContextAccessor)
        { 
            _httpContextAccessor = httpContextAccessor;
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
        [HttpGet]
        private async Task<IstekSayilariDto> IstekSayisiGetirAsync()
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
                    return new IstekSayilariDto();
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Account/getrequestcounts";

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var xdoc = XDocument.Parse(xml);
                        var dataElement = xdoc.Descendants("Data").FirstOrDefault();

                        if (dataElement == null)
                            return new IstekSayilariDto();

                        var dto = new IstekSayilariDto
                        {
                            HesapIstekleri = ParseInt(dataElement.Element("AccountRequests")),
                            KartIstekleri = ParseInt(dataElement.Element("CardRequests")),
                            DestekIstekleri = ParseInt(dataElement.Element("SupportRequests")),
                            LimitArtirmaIstekleri = ParseNullableInt(dataElement.Element("LimitIncreaseRequests"))
                        };

                        return dto;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("XML çözümleme hatası: " + ex.Message);
                        return new IstekSayilariDto();
                    }
                }

                return new IstekSayilariDto();
            }
        }
        private int ParseInt(XElement? element)
        {
            return int.TryParse(element?.Value, out int result) ? result : 0;
        }

        private int? ParseNullableInt(XElement? element)
        {
            if (element == null || string.IsNullOrWhiteSpace(element.Value))
                return null;

            return int.TryParse(element.Value, out int result) ? result : null;
        }

        [HttpGet]
        private async Task<List<GirisOlayi>> GirisOlayiGetirAsync()
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
                    return new List<GirisOlayi>(); 
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var apiUrl = StaticSettings.ApiBaseUrl + "v2/LoginEvent/getall";

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<SuccessDataResult<List<GirisOlayi>>>(json);

                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    };

                    return data.Data ?? new List<GirisOlayi>();
                }

                return new List<GirisOlayi>();
            }
        }
        [HttpGet]
        public async Task<IActionResult> LoginOlaylariHtml()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler);
            var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];

            if (string.IsNullOrEmpty(token))
                return Content("<p>Yetkilendirme hatası</p>", "text/html");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var apiUrl = StaticSettings.ApiBaseUrl + "v2/LoginEvent/getall";

            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                return Content(html, "text/html");
            }

            return Content("<p>Veri alınamadı</p>", "text/html");
        }



        [RoleAuthorize("Administrator")]
        public async Task<ActionResult> Index()
        {
            var veri = await IstekSayisiGetirAsync();
            /*
            var girislog =await GirisOlayiGetirAsync();
            var log = girislog.Select(g => new KullaniciLogDto
            {
             Basarili=g.Basarili,
             Id=g.Id,
             IpAdresi=g.IpAdresi,   
             KullaniciId=g.KullaniciId,
             Zaman = g.Zaman

            }).ToList();*/
   
            var model = new DashboardOzet
            {
                BekleyenHesapIstekleri = veri.HesapIstekleri,
                BekleyenKartIstekleri = veri.KartIstekleri,
                AcikDestekTalepleri = veri.DestekIstekleri,
                BekleyenLimitArtirmaIstekleri = veri.LimitArtirmaIstekleri ?? 0,
               //KullaniciLoglari= log,//
                
            };

            return View(model);
        }
    }
}

using BankaMVC.Filters;
using BankaMVC.Models.DTOs;
using BankaMVC.Models.Result;
using BankaMVC.Models.Somut;
using BankaMVC.Models.Somut.BankaYonetimPaneli.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using static BankaMVC.XmlConverter.XmlConverter;

namespace BankaMVC.Controllers
{
    public class LimitArtirmaController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public LimitArtirmaController(IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
        }
        [RoleAuthorize("Administrator")]
        public async Task<ActionResult> Index()
        {
            var result = await LimitArtirmaIstekleriGetirAsync();

            var model = result.Select(r => new LimitArtirmaIstegi
            {
               BasvuruTarihi=r.BasvuruTarihi,
                Durum = Enum.TryParse<IstekDurumu>(r.Durum, out var parsedDurum) ? parsedDurum : IstekDurumu.Pending,
               KartNo=r.KartNo,
               MevcutLimit=r.MevcutLimit!.Value,
               MusteriAdiSoyadi=r.AdSoyad,
               TalepEdilenLimit=r.TalepEdilenLimit!.Value,
               MusteriId=r.KullaniciId,
               Id=r.Id,    
               
            }).ToList();

            return View(model);

        }
        private async Task<LimitArtırmaDto> LimitArtirmaGetirAsync(int id)
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
                    return new LimitArtırmaDto(); // Token yoksa boş nesne dön
                }

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var apiUrl = StaticSettings.ApiBaseUrl + "LimitIncrease/getbyid/" + id;

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var xmlString = await response.Content.ReadAsStringAsync();

                    var xdoc = XDocument.Parse(xmlString);

                    // Namespace varsa al
                    XNamespace ns = xdoc.Root?.Name.Namespace ?? "";

                    var dataElement = xdoc.Root?.Element(ns + "Data");
                    if (dataElement == null)
                        return new LimitArtırmaDto();

                    var dto = new LimitArtırmaDto
                    {
                        Id = (int?)dataElement.Element(ns + "Id") ?? 0,
                        KullaniciId = (int?)dataElement.Element(ns + "UserId") ?? 0,
                        AdSoyad= (string?)dataElement.Element(ns + "FullName") ?? string.Empty,
                        KartNo = (string?)dataElement.Element(ns + "CardNumber") ?? string.Empty,
                        MevcutLimit = (decimal?)dataElement.Element(ns + "CurrentLimit"),
                        TalepEdilenLimit = (decimal?)dataElement.Element(ns + "RequestedLimit"),
                        BasvuruTarihi = (DateTime?)dataElement.Element(ns + "ApplicationDate") ?? DateTime.MinValue,
                        Durum = (string?)dataElement.Element(ns + "Status") ?? string.Empty
                    };

                    return dto;
                }

                return new LimitArtırmaDto();
            }
        }

        [HttpGet]
        private async Task<List<LimitArtırmaDto>> LimitArtirmaIstekleriGetirAsync()
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
                    return new List<LimitArtırmaDto>(); 
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "LimitIncrease/getcardlimitrequests";

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var xmlString = await response.Content.ReadAsStringAsync();
                    var xdoc = XDocument.Parse(xmlString);

                    var dataElements = xdoc.Descendants("LimitIncreaseDto");

                    var list = dataElements.Select(x => new LimitArtırmaDto
                    {
                        Id = (int?)x.Element("Id") ?? 0,
                        KullaniciId = (int?)x.Element("UserId") ?? 0,
                        AdSoyad = (string?)x.Element("FullName") ?? string.Empty,
                         KartNo = (string?)x.Element("CardNumber") ?? string.Empty,
                        MevcutLimit = (decimal?)x.Element("CurrentLimit") ?? 0m,
                        TalepEdilenLimit = (decimal?)x.Element("RequestedLimit") ?? 0m,
                        BasvuruTarihi = (DateTime?)x.Element("ApplicationDate") ?? DateTime.MinValue,
                        Durum = (string?)x.Element("Status") ?? string.Empty
                    }).ToList();

                    return list;
                }

                return new List<LimitArtırmaDto>(); 
            }
        }




        public async Task<ActionResult> Detay(int id)
        {
            var r = await LimitArtirmaGetirAsync(id);
            if (r == null)
            {
                return HttpNotFound();
            }
            var limitartirmaistegi = new LimitArtirmaIstegi 
            {
                BasvuruTarihi = r.BasvuruTarihi,
                Durum = Enum.TryParse<IstekDurumu>(r.Durum, out var parsedDurum) ? parsedDurum : IstekDurumu.Pending,
                KartNo = r.KartNo,
                MevcutLimit = r.MevcutLimit!.Value,
                MusteriAdiSoyadi = r.AdSoyad,
                TalepEdilenLimit = r.TalepEdilenLimit!.Value,
                MusteriId = r.KullaniciId,
                Id = r.Id,
            };
            return View(limitartirmaistegi);
        }
        [HttpPost("sil/{id}")]
        public async Task<IActionResult> Sil(int id)
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
                    TempData["Error"] = "Giriş yapmanız gerekiyor.";
                    return RedirectToAction("Giris", "Hesap");
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

 
                var apiUrl = $"{StaticSettings.ApiBaseUrl}LimitIncrease/delete/{id}";


                var response = await client.DeleteAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Silme işlemi başarılı.";
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Hata Kodu: " + response.StatusCode);
                    Console.WriteLine("Hata Açıklaması: " + response.ReasonPhrase);
                    Console.WriteLine("Hata İçeriği: " + errorContent);
                    TempData["Error"] = "Silme işlemi başarısız oldu.";
                }

                return RedirectToAction("Index");
            }
        }

        private ActionResult HttpNotFound()
        {
            throw new NotImplementedException();
        }



        [HttpPost]
        public async Task<ActionResult> Onayla(int id, decimal onaylananLimit, string kartNo, decimal mevcutLimit)
        {
            var veri = new LimitArtirmaEkleDto
            {
                Id = id,
                TalepEdilenLimit = onaylananLimit,
                Durum = "Active",
                KartNo = kartNo,
                MevcutLimit = mevcutLimit,
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

                var apiUrl = StaticSettings.ApiBaseUrl + "LimitIncrease/updatelimit";
                
         
                string xmlString;
                var serializer = new XmlSerializer(typeof(LimitArtirmaEkleDto));
                using (var sw = new Utf8StringWriter())
                {
                    serializer.Serialize(sw, veri);
                    xmlString = sw.ToString();
                }

                var content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

                var response = await client.PutAsync(apiUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Your request has been created successfully.";
                    return RedirectToAction("Index");
                }
                else
                {

                    return RedirectToAction("Index");
                }
            }
        }


        [HttpPost]
        public async Task<ActionResult> Reddet(int id, decimal onaylananLimit, string kartNo)
        {
            var veri = new LimitArtirmaEkleDto
            {
                Id = id,
                TalepEdilenLimit = onaylananLimit,
                Durum = "Rejected",
                KartNo = kartNo,
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

                var apiUrl = StaticSettings.ApiBaseUrl + "LimitIncrease/updatelimit";

       
                string xmlString;
                var serializer = new XmlSerializer(typeof(LimitArtirmaEkleDto));
                using (var sw = new Utf8StringWriter())
                {
                    serializer.Serialize(sw, veri);
                    xmlString = sw.ToString();
                }

                var content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

                var response = await client.PutAsync(apiUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Talebiniz başarıyla oluşturuldu.";
                    return RedirectToAction("Index");
                }
                else
                {
                    Console.WriteLine("Hata Kodu: " + response.StatusCode);
                    Console.WriteLine("Hata Açıklaması: " + response.ReasonPhrase);
                    Console.WriteLine("Hata İçeriği: " + responseContent);
                    TempData["Error"] = "Talep oluşturulurken bir hata oluştu.";
                    return RedirectToAction("Index");
                }
            }
        }


    }
}

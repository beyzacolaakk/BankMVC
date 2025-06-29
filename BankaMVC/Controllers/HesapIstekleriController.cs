using BankaMVC.Filters;
using BankaMVC.Models.DTOs;
using BankaMVC.Models.Result;
using BankaMVC.Models.Somut;
using BankaMVC.Models.Somut.BankaYonetimPaneli.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BankaMVC.Controllers
{
    public class HesapIstekleriController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public HesapIstekleriController(IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
        }
        [RoleAuthorize("Administrator")]
        public async Task<ActionResult> Index()
        {
            var result = await HesaplariGetirAsync();
          var model = result.Select(r => new HesapAcmaIstegi
    {
        BasvuruTarihi = r.BasvuruTarihi,
              Durum = Enum.TryParse<IstekDurumu>(r.Durum, out var parsedDurum) ? parsedDurum : IstekDurumu.Pending, // varsayılan Bekliyor
              Telefon = r.Telefon,
        Id = r.Id,
        MusteriAdi = r.AdSoyad,
        MusteriSoyadi = "", // Gerekirse ekle
        HesapNo=r.HesapNo,
        Eposta=r.Eposta,
       
    }).ToList();
            return View(model);
        }
     
        public async Task<ActionResult> Detay(int id)
        {
            var r = await HesapGetirAsync(id);

            if (r == null)
            {
                return HttpNotFound();
            }

            var model = new HesapAcmaIstegi
            {
                BasvuruTarihi = r.BasvuruTarihi,
                Durum = Enum.TryParse<IstekDurumu>(r.Durum, out var parsedDurum) ? parsedDurum : IstekDurumu.Pending,
                Telefon = r.Telefon,
                Id = r.Id,
                MusteriAdi = r.AdSoyad,
                MusteriSoyadi = "", // Gerekirse ekle
                Eposta = r.Eposta,
                HesapNo = r.HesapNo,
            };

            return View(model);
        }
        [HttpGet]
        private async Task<HesapIstekleriDto> HesapGetirAsync(int id)
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
                    return new HesapIstekleriDto(); // Token yoksa boş döner
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Account/getbyid/" + id;

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();
                    var doc = XDocument.Parse(xml);

                    var dataElement = doc.Root?.Element("Data");
                    if (dataElement == null)
                        return new HesapIstekleriDto();

                    var hesap = new HesapIstekleriDto
                    {
                        Id = (int?)dataElement.Element("Id") ?? 0,
                        AdSoyad = (string?)dataElement.Element("FullName") ?? string.Empty,
                        HesapNo = (string?)dataElement.Element("AccountNumber") ?? string.Empty,
                        Durum = (string?)dataElement.Element("Status") ?? string.Empty,
                        BasvuruTarihi = (DateTime?)dataElement.Element("ApplicationDate") ?? DateTime.MinValue,
                        Eposta = (string?)dataElement.Element("Email") ?? string.Empty,
                        Telefon = (string?)dataElement.Element("PhoneNumber") ?? string.Empty,
                    };

                    return hesap;
                }

                return new HesapIstekleriDto();
            }
        }


        [HttpGet]
        private async Task<List<HesapIstekleriDto>> HesaplariGetirAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];
                if (string.IsNullOrEmpty(token))
                    return new List<HesapIstekleriDto>();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Account/getaccountrequests";
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();
                    var doc = XDocument.Parse(xml);

                    // Öncelikle <Data> elementini alıyoruz
                    var dataElement = doc.Root?.Element("Data");

                    var hesapListesi = dataElement?
                        .Elements("AccountRequestDto")
                        .Select(x => new HesapIstekleriDto
                        {
                            Id = (int?)x.Element("Id") ?? 0,
                            AdSoyad = (string?)x.Element("FullName") ?? string.Empty,
                            HesapNo = (string?)x.Element("AccountNumber") ?? string.Empty,
                            Durum = (string?)x.Element("Status") ?? string.Empty,
                            BasvuruTarihi = (DateTime?)x.Element("ApplicationDate") ?? DateTime.MinValue,
                            Eposta = (string?)x.Element("Email") ?? string.Empty,
                            Telefon = (string?)x.Element("PhoneNumber") ?? string.Empty,
                        })
                        .ToList() ?? new List<HesapIstekleriDto>();

                    return hesapListesi;
                }

                return new List<HesapIstekleriDto>();
            }
        }





        private ActionResult HttpNotFound()
        {
            throw new NotImplementedException();
        }


        [HttpPost]
        public async Task<ActionResult> Onayla(int id)
        {
            var veri = new DurumuGuncelleDto
            {
                Id = id,
                Durum = "Active",
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

                // XML'i UTF-8 olarak serialize et
                string xmlString;
                var serializer = new XmlSerializer(typeof(DurumuGuncelleDto));
                using (var ms = new MemoryStream())
                {
                    var settings = new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                        OmitXmlDeclaration = false
                    };

                    using (var writer = XmlWriter.Create(ms, settings))
                    {
                        serializer.Serialize(writer, veri);
                    }

                    xmlString = Encoding.UTF8.GetString(ms.ToArray());
                }

                var content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

                var response = await client.PutAsync(StaticSettings.ApiBaseUrl + "Account/updateaccountstatus", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Talebiniz başarıyla oluşturuldu.";
                    return RedirectToAction("Index");
                }
                else
                {
                    Console.WriteLine($"Hata Kodu: {response.StatusCode}");
                    Console.WriteLine($"Hata Açıklaması: {response.ReasonPhrase}");
                    Console.WriteLine($"Hata İçeriği: {responseContent}");

                    TempData["Error"] = "Talep oluşturulurken bir hata oluştu.";
                    return RedirectToAction("Index");
                }
            }
        }

        [HttpPost]
        public async Task<ActionResult> Reddet(int id)
        {
            var veri = new DurumuGuncelleDto
            {
                Id = id,
                Durum = "Rejected",
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

                string xmlString;
                var serializer = new XmlSerializer(typeof(DurumuGuncelleDto));
                using (var ms = new MemoryStream())
                {
                    var settings = new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                        OmitXmlDeclaration = false
                    };

                    using (var writer = XmlWriter.Create(ms, settings))
                    {
                        serializer.Serialize(writer, veri);
                    }

                    xmlString = Encoding.UTF8.GetString(ms.ToArray());
                }

                var content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

                var response = await client.PutAsync(StaticSettings.ApiBaseUrl + "Account/updateaccountstatus", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Talebiniz başarıyla oluşturuldu.";
                    return RedirectToAction("Index");
                }
                else
                {
                    Console.WriteLine($"Hata Kodu: {response.StatusCode}");
                    Console.WriteLine($"Hata Açıklaması: {response.ReasonPhrase}");
                    Console.WriteLine($"Hata İçeriği: {responseContent}");

                    TempData["Error"] = "Talep oluşturulurken bir hata oluştu.";
                    return RedirectToAction("Index");
                }
            }
        }

    }
}

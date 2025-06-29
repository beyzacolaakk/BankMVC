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
    public class KartIstekleriController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public KartIstekleriController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        [RoleAuthorize("Administrator")]
        public async Task<ActionResult> Index()
        {
            var result = await KartlariGetirAsync();

            var model = result.Select(r => new KartAcmaIstegi
            {
                BasvuruTarihi = r.Tarih,
                Durum = Enum.TryParse<IstekDurumu>(r.Durum, out var parsedDurum) ? parsedDurum : IstekDurumu.Pending,
                Id = r.Id,
                IslemTarihi = DateTime.Now,
                IslemYapan = "ahmet",
                KartTipi = r.KartTipi switch
                {
                    "Credit" => KartTipi.Credit,
                    "Bank" => KartTipi.Bank,
                    _ => KartTipi.Credit
                },

                MusteriAdiSoyadi = r.AdSoyad,
                TalepEdilenLimit = r.Limit ?? 0, // null kontrolü
                MusteriId = r.Id
            }).ToList();

            return View(model);
        }

        public async Task<ActionResult> Detay(int id)
        {
            var k = await KartGetirAsync(id);

             
            if (k == null)
            {
                return HttpNotFound();
            }
            var kartacamaistegi = new KartAcmaIstegi 
            {
                BasvuruTarihi = k.Tarih,
                Durum = Enum.TryParse<IstekDurumu>(k.Durum, out var parsedDurum) ? parsedDurum : IstekDurumu.Pending,
                Id = k.Id,
                IslemTarihi = DateTime.Now,
                IslemYapan = "ahmet",
                KartTipi = k.KartTipi switch
                {
                    "Credit" => KartTipi.Credit,
                    "Bank" => KartTipi.Bank,
                    _ => KartTipi.Credit
                },

                MusteriAdiSoyadi = k.AdSoyad,
                TalepEdilenLimit = k.Limit ?? 0, 
                MusteriId = k.Id
            };
            return View(kartacamaistegi);
        }
        [HttpGet]
        private async Task<KartIstekleriDto> KartGetirAsync(int id)
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
                    return new KartIstekleriDto();
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Card/getbyid/" + id;

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();
                    var doc = XDocument.Parse(xml);

                    var dataElement = doc.Root?.Element("Data");
                    if (dataElement == null)
                        return new KartIstekleriDto();

                    var kart = new KartIstekleriDto
                    {
                        Id = (int?)dataElement.Element("Id") ?? 0,
                        KartTipi = (string?)dataElement.Element("CardType") ?? string.Empty,
                        AdSoyad= (string?)dataElement.Element("FullName") ?? string.Empty,
                        Limit = decimal.TryParse((string?)dataElement.Element("Limit"), out var lim) ? lim : 0,
                        Tarih = DateTime.TryParse((string?)dataElement.Element("ExpirationDate"), out var dt) ? dt : DateTime.MinValue,
                        Durum = (string?)dataElement.Element("Status") ?? string.Empty,
                    };

                    return kart;
                }

                return new KartIstekleriDto();
            }
        }


        [HttpGet]
        private async Task<List<KartIstekleriDto>> KartlariGetirAsync()
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
                    return new List<KartIstekleriDto>(); // Token yoksa boş liste döner
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Card/getcardrequests";

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();
                    var doc = XDocument.Parse(xml);

                    var kartListesi = doc.Root?
                        .Element("Data")?
                        .Elements("CardRequestDto")
                        .Select(x => new KartIstekleriDto
                        {
                            Id = (int?)x.Element("Id") ?? 0,
                            AdSoyad = (string?)x.Element("FullName") ?? string.Empty,
                            KartTipi = (string?)x.Element("CardType") ?? string.Empty,
                            Limit = decimal.TryParse((string?)x.Element("Limit"), out var lim) ? lim : 0,
                            Tarih = DateTime.TryParse((string?)x.Element("ExpirationDate"), out var dt) ? dt : DateTime.MinValue,
                            Durum = (string?)x.Element("Status") ?? string.Empty,
                        })
                        .ToList() ?? new List<KartIstekleriDto>();

                    return kartListesi;
                }

                return new List<KartIstekleriDto>();
            }
        }


        private ActionResult HttpNotFound()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult> Active(int id)
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

                // XML'e UTF-8 olarak çevir
                string xmlString;
                var serializer = new XmlSerializer(typeof(DurumuGuncelleDto));
                using (var sw = new Utf8StringWriter()) // ← burada fark var
                {
                    serializer.Serialize(sw, veri);
                    xmlString = sw.ToString();
                }

                var content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

                var apiUrl = StaticSettings.ApiBaseUrl + "Card/updatecardstatus";
                var response = await client.PutAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

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



        [HttpPost]
        public async Task<ActionResult> Rejected(int id)
        {
            var veri = new DurumuGuncelleDto
            {
                Id = id,
                Durum = "Rejected"
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
                using (var sw = new Utf8StringWriter()) 
                {
                    serializer.Serialize(sw, veri);
                    xmlString = sw.ToString();
                }

                var content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

                var apiUrl = StaticSettings.ApiBaseUrl + "Card/updatecardstatus";
                var response = await client.PutAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

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


using Banka.Varlıklar.DTOs;
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

    public class DestekTalepleriController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public DestekTalepleriController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor; 
        }
        [RoleAuthorize("Administrator")]
        public async Task<ActionResult> Index()
        {
            var result = await DestekTalebleriGetirAsync();
            var model = result.Select(r => new DestekTaleb
            {
                Durum= Enum.TryParse<DestekDurumu>(r.Durum, out var parsedDurum) ? parsedDurum : DestekDurumu.Closed,
                Aciklama=r.Mesaj,
                Konu=r.Konu,
                MusteriAdiSoyadi=r.AdSoyad,
                MusteriId=r.KullaniciId,
                TalepTarihi=r.Tarih.Value,
                Yanit=r.Yanit,
                 Id = r.Id
                

            }).ToList();
            return View(model);
        }


        public async Task<ActionResult> Detay(int id)
        {
            var d = await DestekTalebiGetirAsync(id);
            if (d == null)
            {
                return HttpNotFound();
            }
            var destekTaleb = new DestekTaleb
            {
                Durum = Enum.TryParse<DestekDurumu>(d.Durum, out var parsedDurum) ? parsedDurum : DestekDurumu.Closed,
                Aciklama = d.Mesaj,
                Konu = d.Konu,
                MusteriAdiSoyadi = d.AdSoyad,
                MusteriId = d.KullaniciId,
                TalepTarihi = d.Tarih.Value,
                Yanit=d.Yanit,
                Id = d.Id

            };
            return View(destekTaleb);
        }
        [HttpGet]
        private async Task<DestekTalebiOlusturDto> DestekTalebiGetirAsync(int id)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];

                if (string.IsNullOrEmpty(token))
                    return new DestekTalebiOlusturDto();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "SupportRequest/getbyid/" + id;

                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                    return new DestekTalebiOlusturDto();

                var xmlString = await response.Content.ReadAsStringAsync();

                var xdoc = XDocument.Parse(xmlString);

                // Root -> Data elementini bul
                var dataElement = xdoc.Root?.Element("Data");
                if (dataElement == null)
                    return new DestekTalebiOlusturDto();
                var dto = new DestekTalebiOlusturDto
                {
                    Id = (int?)dataElement.Element("id") ?? 0,
                    KullaniciId = (int?)dataElement.Element("userId") ?? 0,
                    Konu = (string)dataElement.Element("subject") ?? "",
                    Mesaj = (string)dataElement.Element("message") ?? "",
                    Durum = (string)dataElement.Element("status") ?? "",
                    Yanit = (string)dataElement.Element("response") ?? "",
                    Kategori = (string)dataElement.Element("category") ?? "",
                    AdSoyad = (string)dataElement.Element("fullName") ?? "",
                    Tarih = ((DateTime?)dataElement.Element("createdDate") ?? DateTime.MinValue).Date


                };

                return dto;
            }
        }
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Silme(int id)
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
              
                    return RedirectToAction("Giris", "Hesap");
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml")); 

                var apiUrl = $"{StaticSettings.ApiBaseUrl}SupportRequest/delete/{id}";

         
                var response = await client.DeleteAsync(apiUrl);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Silme işlemi başarılı.";
                }
                else
                {
        
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
        public async Task<ActionResult> DurumGuncelle(int id, DestekDurumu durum, string yanit)
        {
            var veri = new SupportRequestUpdateDto
            {
                Id = id,
                Status = durum.ToString(),
                Response = yanit
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

                // Serialize to XML with correct root name
                string xmlString;
                var serializer = new XmlSerializer(typeof(SupportRequestUpdateDto), new XmlRootAttribute("SupportRequest"));

                using (var sw = new Utf8StringWriter())
                {
                    serializer.Serialize(sw, veri);
                    xmlString = sw.ToString();
                }

                var content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

                var apiUrl = StaticSettings.ApiBaseUrl + "SupportRequest/updatesupportrequest";

                var response = await client.PutAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Talep başarıyla güncellendi.";
                    return RedirectToAction("Index");
                }
                else
                {
                    Console.WriteLine("Hata Kodu: " + response.StatusCode);
                    Console.WriteLine("Hata Açıklaması: " + response.ReasonPhrase);
                    Console.WriteLine("Hata İçeriği: " + responseContent);
                    TempData["Error"] = "Talep güncellenirken bir hata oluştu.";
                    return RedirectToAction("Index");
                }
            }
        }

        [HttpGet]
        private async Task<List<DestekTalebiOlusturDto>> DestekTalebleriGetirAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["UserJwtToken"];

                if (string.IsNullOrEmpty(token))
                    return new List<DestekTalebiOlusturDto>();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "SupportRequest/getsupportrequests";

                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                    return new List<DestekTalebiOlusturDto>();

                var xmlString = await response.Content.ReadAsStringAsync();

                var xdoc = XDocument.Parse(xmlString);

                // Data içindeki SupportRequestDto elementlerini alıyoruz
                var dataElements = xdoc.Root?
                    .Element("Data")?
                    .Elements("SupportRequestDto");

                var liste = new List<DestekTalebiOlusturDto>();

                if (dataElements != null)
                {
                    foreach (var elem in dataElements)
                    {
                        var destek = new DestekTalebiOlusturDto
                        {
                            Id = (int?)elem.Element("id") ?? 0,
                            KullaniciId = (int?)elem.Element("userId") ?? 0,
                             Konu = (string)elem.Element("subject") ?? "",
                           Mesaj = (string)elem.Element("message") ?? "",
                            Durum = (string)elem.Element("status") ?? "",
                            Yanit = (string)elem.Element("response") ?? "",
                            Kategori = (string)elem.Element("category") ?? "",
                             AdSoyad = (string)elem.Element("fullName") ?? "",
                            Tarih = (DateTime?)elem.Element("date") ?? DateTime.MinValue
                        };

                        liste.Add(destek);
                    }
                }

                return liste;
            }
        }



    }
}


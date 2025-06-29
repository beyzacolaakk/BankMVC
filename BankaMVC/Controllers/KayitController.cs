using BankaMVC.Models.Result;
using BankaMVC.Models.Somut;
using BankaMVC.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using static BankaMVC.Controllers.GirisController;
using System.Security.Claims;
using System.Text;
using Banka.Varlıklar.DTOs;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BankaMVC.Controllers
{
    public class KayitController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public KayitController(IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IActionResult> Index()
        {
            var token = Request.Cookies["UserJwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

                if (roleClaim != null)
                {
                    var userRole = roleClaim.Value;

                    if (userRole.Equals("Yönetici", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "AdminPanel");
                    }
                    else if (userRole.Equals("Müşteri", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "GostergePaneli");
                    }
                }
                return RedirectToAction("Index", "GostergePaneli");
            }
            KayitViewModel kayit=new KayitViewModel();
            kayit.Subeler = await SubelerGetirAsync();
            return View(kayit);
        }
        private async Task<List<SubeDto>> SubelerGetirAsync()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (var client = new HttpClient(handler))
            {
                var apiUrl = StaticSettings.ApiBaseUrl + "Branch/getbranches";
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var document = XDocument.Parse(xml);
                        var subeler = new List<SubeDto>();

                        var elements = document.Descendants("BranchDto");

                        foreach (var element in elements)
                        {
                            var dto = new SubeDto
                            {
                                Id = int.TryParse(element.Element("Id")?.Value, out var id) ? id : 0,
                                SubeAdi = element.Element("BranchName")?.Value ?? string.Empty
                            };

                            subeler.Add(dto);
                        }

                        return subeler;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("XML parse hatası: " + ex.Message);
                        return new List<SubeDto>();
                    }
                }

                return new List<SubeDto>();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Kayit(KayitViewModel kayitViewModel)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

                kayitViewModel.Subeler = await SubelerGetirAsync();
                return View("Basarisiz", kayitViewModel);
            }

            var dto = new UserRegisterDto
            {
                FullName = kayitViewModel.AdSoyad,
                Email = kayitViewModel.Email,
                Phone = "0" + kayitViewModel.Telefon,
                Password = kayitViewModel.Sifre,
                Branch = kayitViewModel.SecilenSubeId,
                IsActive = true
            };


            var xmlSerializer = new XmlSerializer(typeof(UserRegisterDto));
            string xmlContent;
            using (var stringWriter = new Utf8StringWriter())  
            {
                xmlSerializer.Serialize(stringWriter, dto);
                xmlContent = stringWriter.ToString();
            }

            using var client = new HttpClient();

            // XML içerikli request oluştur
            var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");

            var response = await client.PostAsync(StaticSettings.ApiBaseUrl + "v1/Auth/register", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", "Giris");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, "Kayıt işlemi başarısız: " + error);
            kayitViewModel.Subeler = await SubelerGetirAsync();
            return View("Basarisiz", kayitViewModel);
        }

        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }





    }
}

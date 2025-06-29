using BankaMVC.Filters;
using BankaMVC.Models.DTOs;
using BankaMVC.Models.Result;
using BankaMVC.Models.Somut;
using BankaMVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BankaMVC.Controllers
{
    public class SonHareketlerController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public SonHareketlerController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        [RoleAuthorize("Customer")]
        public async Task<IActionResult> Index(string tab = "vadesiz")
        {
            var model = new SonHareketlerViewModel
            {
                AktifTab = tab,
                HesapIslemler = await SonHaraketlerHesapGetirAsync(),
                KrediKartiIslemleri = await SonHaraketlerKartGetirAsync(),
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetIslemler(string tip)
        {
            if (tip == "kredi")
            {
                var krediIslemleri =await SonHaraketlerKartGetirAsync();
                return PartialView("_IslemListesi", krediIslemleri);
            }
            else
            {
                var hesapIslemler = await SonHaraketlerHesapGetirAsync();
                return PartialView("_IslemListesi", hesapIslemler);
            }
        }
        [HttpGet]
        private async Task<List<SonHareketlerDto>> SonHaraketlerHesapGetirAsync()
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
                    return new List<SonHareketlerDto>();
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var apiUrl = StaticSettings.ApiBaseUrl + "Transaction/getlast4";

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var document = XDocument.Parse(xml);
                        var hareketler = new List<SonHareketlerDto>();


                        var elements = document.Descendants("TransactionSummaryDto");

                        foreach (var element in elements)
                        {
                            var dto = new SonHareketlerDto
                            {
                                GuncelBakiye = decimal.Parse(element.Element("CurrentBalance")?.Value ?? "0"),
                                Tutar = decimal.Parse(element.Element("Amount")?.Value ?? "0"),
                                IslemTipi = element.Element("TransactionType")?.Value ?? string.Empty,
                                Tarih = DateTime.Parse(element.Element("Date")?.Value ?? DateTime.MinValue.ToString()),
                                Aciklama = "", 
                                Durum = element.Element("Status")?.Value ?? string.Empty
                            };

                            hareketler.Add(dto);
                        }

                        return hareketler;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("XML işleme hatası: " + ex.Message);
                        return new List<SonHareketlerDto>();
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Hata Kodu: " + response.StatusCode);
                    Console.WriteLine("Hata Açıklaması: " + response.ReasonPhrase);
                    Console.WriteLine("Hata İçeriği: " + errorContent);
                    TempData["Error"] = "Talep oluşturulurken bir hata oluştu.";
                    return new List<SonHareketlerDto>();
                }
            }
        }



[HttpGet]
private async Task<List<SonHareketlerDto>> SonHaraketlerKartGetirAsync()
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
            return new List<SonHareketlerDto>();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml")); // XML beklentisi

        var apiUrl = StaticSettings.ApiBaseUrl + "CardTransaction/getlast4transactions";

        var response = await client.GetAsync(apiUrl);
        if (response.IsSuccessStatusCode)
        {
            var xml = await response.Content.ReadAsStringAsync();

            try
            {
                var document = XDocument.Parse(xml);
                var hareketler = new List<SonHareketlerDto>();

                // TransactionSummaryDto öğeleri <Data> içinde geliyor
                var items = document.Descendants("TransactionSummaryDto");

                foreach (var item in items)
                {
                    var dto = new SonHareketlerDto
                    {
                        GuncelBakiye = decimal.Parse(item.Element("CurrentBalance")?.Value ?? "0"),
                        Tutar = decimal.Parse(item.Element("Amount")?.Value ?? "0"),
                        IslemTipi = item.Element("TransactionType")?.Value ?? string.Empty,
                        Tarih = DateTime.Parse(item.Element("Date")?.Value ?? DateTime.MinValue.ToString()),
                        Aciklama = "", // XML'de Aciklama yoksa boş bırak
                        Durum = item.Element("Status")?.Value ?? string.Empty
                    };

                    hareketler.Add(dto);
                }

                return hareketler;
            }
            catch (Exception ex)
            {
                Console.WriteLine("XML parse hatası: " + ex.Message);
                return new List<SonHareketlerDto>();
            }
        }

        return new List<SonHareketlerDto>();
    }
}



    }
}

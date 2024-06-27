using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Zebra_RFID_Scanner_Android.Models;
using Zebra_RFID_Scanner_Android.Services;
using Zebra_RFID_Scanner_Android.Views;

namespace Zebra_RFID_Scanner_Android.Api
{
    public class PKLApi
    {
        private readonly IAuthenticationService _authenticationService;

        public PKLApi(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }
        public async Task<string> CallApi(string apiUrl)
        {
            string sessionId = _authenticationService.SessionId;
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Add("Cookie", "ASP.NET_SessionId=" + sessionId + "");
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                if (sessionId == null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }

                return await response.Content.ReadAsStringAsync();
            }
        }
        public async Task<string> GetFileApi(string apiUrl,string portCode,string date)
        {
            string sessionId = _authenticationService.SessionId;
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("Cookie", "ASP.NET_SessionId=" + sessionId + "");
                var content = new MultipartFormDataContent();
                content.Add(new StringContent(portCode), "portCode");
                content.Add(new StringContent(date), "date");
                request.Content = content;
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                if (sessionId == null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
                return await response.Content.ReadAsStringAsync();
            }
        }
        public async Task<string> SaveDataAsync(string apiUrl,bool TypeStatus, string idReports, ObservableCollection<File.FileFormat> Ctn, ObservableCollection<File.FileFormat> ctnError, ObservableCollection<File.EpcReport> epcToUpc, string Po, string So, string Sku,
        string info, string Consignee, string Shipper, string TimeStart, ObservableCollection<File.EPCDiscrepancys> ePCDiscrepancys)
        {
            try
            {
                string sessionId = _authenticationService.SessionId;
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                    request.Headers.Add("Cookie", "ASP.NET_SessionId="+sessionId+"");
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(idReports), "idReports");
                    content.Add(new StringContent(JsonConvert.SerializeObject(Ctn)), "Ctn");
                    if (!TypeStatus)
                    {
                        content.Add(new StringContent(JsonConvert.SerializeObject(ctnError)), "ctnError");
                        content.Add(new StringContent(Consignee), "Consignee");
                        content.Add(new StringContent(Shipper), "Shipper");
                        content.Add(new StringContent(TimeStart), "TimeStart");
                        content.Add(new StringContent(JsonConvert.SerializeObject(ePCDiscrepancys)), "EPCDiscrepancy");
                    }
                    content.Add(new StringContent(JsonConvert.SerializeObject(epcToUpc)), "epcToUpc");
                    content.Add(new StringContent(Po), "Po");
                    content.Add(new StringContent(So), "So");
                    content.Add(new StringContent(Sku), "Sku");
                    content.Add(new StringContent("[]"), "info");
                    request.Content = content;
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch(Exception ex)
            {
                  return "Lỗi "+ex.Message;
            }
        }
    }
}

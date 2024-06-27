using Acr.UserDialogs;
using Android.App;
using Android.Widget;
using Java.Net;
using Java.Util;
using Newtonsoft.Json;
using System;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;

using System.Threading.Tasks;
using Xamarin.Forms;

using Zebra_RFID_Scanner_Android.Droid;
using Zebra_RFID_Scanner_Android.Services;
using Zebra_RFID_Scanner_Android.Views;
using static Zebra_RFID_Scanner_Android.Models.File;
using File = Zebra_RFID_Scanner_Android.Models.File;

namespace Zebra_RFID_Scanner_Android.ViewModels
{
    public class ModalCheckCartonViewModel : BaseViewModel, INotifyPropertyChanged, IIntentService
    {
        private const string EXTENSIONCSV = ".csv";
        private IHostData _hostData;
        private IModalPage modalPage;
        private IProgressDialog progressDialog;
        private readonly IAuthenticationService _authenticationService;

        public new event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<File.FileFormat> CartonRows { get; set; } = new ObservableCollection<File.FileFormat>();
        public ObservableCollection<File.EPCDiscrepancys> ePCDiscrepancys { get; set; } = new ObservableCollection<File.EPCDiscrepancys>();
        public ObservableCollection<string> epcs = new ObservableCollection<string>();
        public ModalCheckCartonViewModel()
        {
            _authenticationService = DependencyService.Get<IAuthenticationService>();
            if (_authenticationService == null || string.IsNullOrEmpty(_authenticationService.SessionId))
            {
                Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
            CartonRows = new ObservableCollection<File.FileFormat>();
            ePCDiscrepancys = new ObservableCollection<File.EPCDiscrepancys>();
        }

        public async Task ReadEx(string Url,string PO, bool TypeStatus)
        {
            try
            {
                progressDialog = UserDialogs.Instance.Loading("Loading...");

                modalPage = DependencyService.Get<IModalPage>();
                modalPage.modalPage = true;
                _hostData = DependencyService.Get<IHostData>();
                CartonRows.Clear();
                ePCDiscrepancys.Clear();
                var client = new HttpClient();

                if (TypeStatus)
                {
                    await FetchDataAsync(1, Url, PO);
                }
                else
                {
                    // Tạo yêu cầu GET đến URL của tệp Excel
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_hostData.HostDatas}/PreASN/" + Url);

                    // Gửi yêu cầu và nhận phản hồi
                    var response = await client.SendAsync(request);

                    // Kiểm tra xem phản hồi có thành công không
                    if (response.IsSuccessStatusCode)
                    {
                        // Đọc dữ liệu từ phản hồi
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            int processedLines = 0;


                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string line;
                                while ((line = await reader.ReadLineAsync()) != null)
                                {
                                    processedLines++;
                                    string[] columns = line.Split(',');
                                    if (columns[0]?.Trim() == "doNo") continue;

                                    var ePCDiscrepancys = new File.EPCDiscrepancys
                                    {
                                        Carton = columns[6]?.Trim(),
                                        Po = columns[15]?.Trim(),
                                        SKU = columns[16]?.Trim(),
                                        So = columns[14]?.Trim(),
                                        UPC = columns[17]?.Trim(),
                                        cntry = columns[10]?.Trim(),
                                        port = columns[9]?.Trim(),
                                        doNo = columns[0]?.Trim(),
                                        setCd = columns[5]?.Trim(),
                                        mngFctryCd = columns[2]?.Trim(),
                                        facBranchCd = columns[3]?.Trim(),
                                        subDoNo = columns[1]?.Trim(),
                                        packKey = columns[12]?.Trim(),
                                        Id = columns[13]?.Trim()
                                    };
                                    await CreateListCSV(ePCDiscrepancys);
                                }
                            }
                          
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to retrieve Excel file. Status code: " + response.StatusCode);
                    }
                    // Close the modal page after processing
                }
            }
            catch (System.Exception ex)
            {
                throw new Exception("An error occurred: " + ex.Message);
            }
            finally
            {
                progressDialog?.Hide();
            }
        }
        public async Task FetchDataAsync(int page,string url,string po)
        {
            try
            {
                var id = url.Replace(EXTENSIONCSV, "");
                string sessionId = _authenticationService.SessionId;
                if (sessionId == null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
                using (HttpClient client = new HttpClient())
                {

                    var info = "FetchEPCDiscrepancyHandheld";
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_hostData.HostDatas}/ReportsUNIQLO/{info}?id={id}&page={page}&po={po}");
                    request.Headers.Add("Cookie", "ASP.NET_SessionId=" + sessionId + "");
                    // Gửi yêu cầu và nhận phản hồi
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<InfoCtn>(content);

                        if (responseData.code == 200)
                        {
                                foreach (var v in responseData.epcToUpc)
                                {
                                    var ePCDiscrepancys = new File.EPCDiscrepancys
                                    {
                                        Carton = v.ctn?.Trim(),
                                        Po = v.po?.Trim(),
                                        SKU = v.sku?.Trim(),
                                        So = v.so?.Trim(),
                                        UPC = v.upc?.Trim(),
                                        cntry = v.cntry?.Trim(),
                                        port = v.dptPortCd?.Trim(),
                                        doNo = v.doNo?.Trim(),
                                        setCd = v.setCd?.Trim(),
                                        subDoNo = v.subDoNo?.Trim(),
                                        packKey = v.packKey?.Trim(),
                                        mngFctryCd = v.mngFctryCd.Trim(),
                                        facBranchCd = v.facBranchCd?.Trim(),
                                        Id = v.EPC?.Trim()
                                    };
                                    await CreateListCSV(ePCDiscrepancys);
                                }
                                if (page < responseData.pages)
                                {
                                    await FetchDataAsync(page + 1,url,po);
                                }
                                else
                                {
                                    await FetchEpcAsync(1,url,po);
                                }
  
                        }
                        else
                        {
                            throw new Exception($"Failed to fetch data: {responseData.msg}");
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to fetch data. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching data: {ex.Message}");
            }
        }
        public async Task FetchEpcAsync(int page,string url,string po)
        {
            try
            {
                var id = url.Replace(EXTENSIONCSV, "");
                string sessionId = _authenticationService.SessionId;
                if (sessionId == null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_hostData.HostDatas}/ReportsUNIQLO/InfoEPCHandheld?id={id}&page={page}&po={po}");
                    request.Headers.Add("Cookie", "ASP.NET_SessionId=" + sessionId + "");
                    // Gửi yêu cầu và nhận phản hồi
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<InfoCtn>(content);

                        if (responseData.code == 200)
                        {
                            foreach (var v in responseData.epcToUpc)
                            {

                                   var ePCDiscrepancys = new File.EPCDiscrepancys
                                    {
                                        Carton = v.ctn?.Trim(),
                                        Po = v.po?.Trim(),
                                        SKU = v.sku?.Trim(),
                                        So = v.so?.Trim(),
                                        UPC = v.upc?.Trim(),
                                        cntry = v.cntry?.Trim(),
                                        port = v.dptPortCd?.Trim(),
                                        doNo = v.doNo?.Trim(),
                                        setCd = v.setCd?.Trim(),
                                        subDoNo = v.subDoNo?.Trim(),
                                        packKey = v.packKey?.Trim(),
                                        mngFctryCd = v.mngFctryCd?.Trim(),
                                        facBranchCd = v.facBranchCd?.Trim(),
                                        Id = v.EPC?.Trim()
                                    };
                                    await CreateListCSV(ePCDiscrepancys);
                                
                               
                                epcs.Add(v.EPC);
                               
                            }
                            if (page < responseData.pages)
                            {
                                await FetchEpcAsync(page + 1,url, po);
                            }
                            else
                            {
                                await MatchCtnRescan(1, url,po);
                            }
                        }
                        else
                        {
                            throw new Exception($"Failed to fetch data: {responseData.msg}");
                        }

                    }
                    else
                    {
                        throw new Exception($"Failed to fetch data. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching data: {ex.Message}");
            }
        }
        public async Task MatchCtnRescan(int page, string url,string po)
        {
            try
            {
                var id = url.Replace(EXTENSIONCSV, "");
                string sessionId = _authenticationService.SessionId;
                if (sessionId == null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_hostData.HostDatas}/ReportsUNIQLO/InfoCtn?id={id}&page={page}&po={{po}}\"");
                    request.Headers.Add("Cookie", "ASP.NET_SessionId=" + sessionId + "");
                    // Gửi yêu cầu và nhận phản hồi
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<InfoCtn>(content);

                        if (responseData.code == 200)
                        {
                            foreach (var v in responseData.Ctn)
                            {

                                    if (v.status == "Matched")
                                    {
                                        UpdateCartonRow(v.ctn);
                                    }
                                
                            }
                            if (page < responseData.pages)
                            {
                                await MatchCtnRescan(page + 1, url, po);
                            }
                        }
                        else
                        {
                            throw new Exception($"Failed to fetch data: {responseData.msg}");
                        }

                    }
                    else
                    {
                        throw new Exception($"Failed to fetch data. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching data: {ex.Message}");
            }
        }
        private async
        Task
        CreateListCSV(File.EPCDiscrepancys ePCDiscrepancy)
        {
            try
            {
                var checkUpc = CartonRows.FirstOrDefault(x => x.CartonTo == ePCDiscrepancy.Carton&&x.PO == ePCDiscrepancy.Po);
                var checkEpc = ePCDiscrepancys.FirstOrDefault(x => x.Id == ePCDiscrepancy.Id);

                if (checkEpc == null)
                {
                    ePCDiscrepancys.Add(new File.EPCDiscrepancys
                    {
                        Carton = ePCDiscrepancy.Carton,
                        Id = ePCDiscrepancy.Id,
                        So = ePCDiscrepancy.So,
                        Po = ePCDiscrepancy.Po,
                        SKU = ePCDiscrepancy.SKU,
                        UPC = ePCDiscrepancy.UPC,
                        cntry = ePCDiscrepancy.cntry,
                        port = ePCDiscrepancy.port,
                        deviceNum = rfidModel.rfidReader.HostName.ToString(),
                        doNo = ePCDiscrepancy.doNo,
                        setCd = ePCDiscrepancy.setCd,
                        subDoNo = ePCDiscrepancy.subDoNo,
                        mngFctryCd = ePCDiscrepancy.mngFctryCd,
                        facBranchCd = ePCDiscrepancy.facBranchCd,
                        packKey = ePCDiscrepancy.packKey,
                    });
                }
                if (checkUpc == null)
                {
                    CartonRows.Add(new File.FileFormat
                    {
                        CartonTo = ePCDiscrepancy.Carton,
                        PO = ePCDiscrepancy.Po,
                        Sku = ePCDiscrepancy.SKU,
                        So = ePCDiscrepancy.So,
                        UPC = ePCDiscrepancy.Carton,
                        Qty = "1",
                        qtyscan = "0",
                        StatusCtn = false,
                        Status = false,
                        cntry = ePCDiscrepancy.cntry,
                        port = ePCDiscrepancy.port,
                        deviceNum = rfidModel.rfidReader.HostName.ToString(),
                        doNo = ePCDiscrepancy.doNo,
                        setCd = ePCDiscrepancy.setCd,
                        subDoNo = ePCDiscrepancy.subDoNo,
                        packKey = ePCDiscrepancy.packKey,
                        mngFctryCd = ePCDiscrepancy.mngFctryCd,
                        facBranchCd = ePCDiscrepancy.facBranchCd,
                    });
                }
                else
                {
                    checkUpc.Qty = (int.Parse(checkUpc.Qty) + 1).ToString();
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Failed :" + ex.Message);
            }

        }

        public void ProcessIntentData(string data)
        {
            // Xử lý dữ liệu nhận được từ Intent
            UpdateCartonRow(data);
        }
        public void UpdateCartonRow(string barcode)
        {
            var check = CartonRows.FirstOrDefault(x => x.CartonTo == barcode);
            if (check != null)
            {
                check.StatusCtn = true;
            }
            else
            {
                Toast.MakeText(Android.App.Application.Context, "Cartons do not match", ToastLength.Short).Show();
            }
        }
        protected new virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

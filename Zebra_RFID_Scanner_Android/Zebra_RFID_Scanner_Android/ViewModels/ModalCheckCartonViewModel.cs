using Acr.UserDialogs;
using Android.App;
using Android.Widget;
using Java.Net;
using Java.Util;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
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
        private const string EXTENSIONEXCEL = ".xlsx";
        private IHostData _hostData;
        private IModalPage modalPage;
        private IProgressDialog progressDialog;
        private readonly IAuthenticationService _authenticationService;
        private readonly Services.ISite _site;

        public new event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<File.FileFormat> CartonRows { get; set; } = new ObservableCollection<File.FileFormat>();
        public ObservableCollection<File.EPCDiscrepancys> ePCDiscrepancys { get; set; } = new ObservableCollection<File.EPCDiscrepancys>();
        public ObservableCollection<string> epcs = new ObservableCollection<string>();
        public ICommand SkipScanCommand { get; private set; }
        private int count;
        public int Count
        {
            get => count;
            set
            {
                if (count != value)
                {
                    count = value;
                    OnPropertyChanged();
                }
            }
        }
        public ModalCheckCartonViewModel()
        {
            _authenticationService = DependencyService.Get<IAuthenticationService>();
            _site = DependencyService.Get<Services.ISite>();
            if (_authenticationService == null || string.IsNullOrEmpty(_authenticationService.SessionId))
            {
                Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
            CartonRows = new ObservableCollection<File.FileFormat>();
            ePCDiscrepancys = new ObservableCollection<File.EPCDiscrepancys>();
            SkipScanCommand = new Command(OnSkipScan);
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
                    if (_site.Sites == "client")
                    {
                        await FetchEpcAsync(1, Url);
                    }
                    else
                    {
                        await FetchDataAsync(1, Url);

                    }
                }
                else
                {
                    //await FetchDataAsync(1, Url, PO);
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
                                using (var package = new ExcelPackage(stream))
                                {
                                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                                    // Lấy sheet đầu tiên từ tệp Excel
                                    var worksheet = package.Workbook.Worksheets[0];
                                    var rowCount = worksheet.Dimension.End.Row;

                                    for (int row = 2; row <= rowCount; row++)
                                    {
                                        var CartonToE = worksheet.Cells[row, 6].Value?.ToString().Trim();
                                        var ConsigneeE = worksheet.Cells[row, 1].Value?.ToString().Trim();
                                        var ShipperE = worksheet.Cells[row, 2].Value?.ToString().Trim();
                                        var POE = worksheet.Cells[row, 4].Value?.ToString().Trim();
                                        var SkuE = worksheet.Cells[row, 5].Value?.ToString().Trim();
                                        var SoE = worksheet.Cells[row, 3].Value?.ToString().Trim();
                                        var upcE = worksheet.Cells[row, 8].Value?.ToString().Trim();
                                        var epcE = worksheet.Cells[row, 7].Value?.ToString().Trim();
                                    EPCDiscrepancys ePCDiscrepancys = new EPCDiscrepancys() { 
                                        Consignee = ConsigneeE,
                                        Shipper = ShipperE,
                                        Carton = CartonToE,
                                        Po = POE,
                                        SKU = SkuE,
                                        So = SoE,
                                        UPC= upcE,
                                        Id = epcE,
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
        public async Task FetchDataAsync(int page,string url)
        {
            try
            {
                var id = url.Replace(EXTENSIONCSV, "").Replace(EXTENSIONEXCEL,"");
                string sessionId = _authenticationService.SessionId;
                if (sessionId == null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_hostData.HostDatas}/Reports/FetchEPCDiscrepancy?id={id}&page={page}");
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
                                        Id = v.EPC?.Trim(),
                                    };
                                    await CreateListCSV(ePCDiscrepancys);
                                }
                                if (page < responseData.pages)
                                {
                                    await FetchDataAsync(page + 1,url);
                                }
                                else
                                {
                                    await FetchEpcAsync(1,url);
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
        public async Task FetchEpcAsync(int page,string url)
        {
            try
            {
                var id = url.Replace(EXTENSIONCSV, "").Replace(EXTENSIONEXCEL, "");
                string sessionId = _authenticationService.SessionId;
                if (sessionId == null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_hostData.HostDatas}/Reports/InfoEPC?id={id}&page={page}");
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
                                        Id = v.EPC?.Trim()
                                    };
                                    await CreateListCSV(ePCDiscrepancys);
                                if(_site.Sites == "client")
                                {
                                    if (v.statusClient)
                                    {
                                        epcs.Add(v.EPC);
                                    }
                                }
                                else
                                {
                                    epcs.Add(v.EPC);
                                }
                               
                            }
                            if (page < responseData.pages)
                            {
                                await FetchEpcAsync(page + 1,url);
                            }
                            else
                            {
                                await MatchCtnRescan(1, url);
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
        public async Task MatchCtnRescan(int page, string url)
        {
            try
            {
                var id = url.Replace(EXTENSIONCSV, "").Replace(EXTENSIONEXCEL, "");
                string sessionId = _authenticationService.SessionId;
                if (sessionId == null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{_hostData.HostDatas}/Reports/InfoCtn?id={id}&page={page}");
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
                                if (_site.Sites == "client")
                                {
                                    if (v.statusClient)
                                    {
                                        UpdateCartonRow(v.ctn);
                                    }
                                }
                                else
                                {
                                    if (v.status == "Matched")
                                    {
                                        UpdateCartonRow(v.ctn);
                                    }
                                   
                                }
                            }
                            if (page < responseData.TotalPages)
                            {
                                await MatchCtnRescan(page + 1, url);
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
                var checkUpc = CartonRows.FirstOrDefault(x => x.UPC == ePCDiscrepancy.UPC && x.CartonTo == ePCDiscrepancy.Carton);
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
                        deviceNum = rfidModel.rfidReader.HostName.ToString(),
                    }) ;
                }
                if (checkUpc == null)
                {
                    CartonRows.Add(new File.FileFormat
                    {
                        CartonTo = ePCDiscrepancy.Carton,
                        PO = ePCDiscrepancy.Po,
                        Sku = ePCDiscrepancy.SKU,
                        So = ePCDiscrepancy.So,
                        UPC = ePCDiscrepancy.UPC,
                        Qty = "1",
                        qtyscan = "0",
                        StatusCtn = false,
                        Status = false,
                        deviceNum = rfidModel.rfidReader.HostName.ToString(),
                        Location = "",
                    });;
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
        private void OnSkipScan()
        {
            CartonRows.ForEach(x =>
            {
                x.StatusCtn = true;
                x.ColorCtn = System.Drawing.Color.Green;
            });
            Count = CartonRows.Count();
            // Your logic here
        }
        public void ProcessIntentData(string data)
        {
            // Xử lý dữ liệu nhận được từ Intent
            UpdateCartonRow(data);
        }
        public void UpdateCartonRow(string barcode)
        {

            var check = CartonRows.FirstOrDefault(x => x.CartonTo == barcode.ToString());
            if (check != null)
            {
                check.StatusCtn = true;
                check.ColorCtn = System.Drawing.Color.Green;
            }
            else
            {
                Toast.MakeText(Android.App.Application.Context, "Cartons do not match", ToastLength.Short).Show();
            }
            var c = CartonRows.Where(x => x.StatusCtn == true).Count();
            Count = c;
            // Lấy các phần tử có Status = true và Status = false
            var statusTrueItems = CartonRows.Where(x => x.StatusCtn).ToList();
            var statusFalseItems = CartonRows.Where(x => !x.StatusCtn).ToList();
            // Xóa tất cả phần tử trong CartonRows
            CartonRows.Clear();

            // Thêm các phần tử có Status = true trước, sau đó là Status = false
            foreach (var item in statusTrueItems)
            {
                CartonRows.Add(item);
            }

            foreach (var item in statusFalseItems)
            {
                CartonRows.Add(item);
            }
        }
        protected new virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

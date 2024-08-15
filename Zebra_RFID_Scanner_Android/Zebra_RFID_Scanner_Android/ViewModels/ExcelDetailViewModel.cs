using Acr.UserDialogs;
using Android.Media;
using Android.Nfc;
using Android.Util;
using Android.Widget;
using Com.Zebra.Rfid.Api3;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Zebra_RFID_Scanner_Android.Api;
using Zebra_RFID_Scanner_Android.Droid;
using Zebra_RFID_Scanner_Android.Models;
using Zebra_RFID_Scanner_Android.Services;
using Zebra_RFID_Scanner_Android.Views;
using static Android.Content.ClipData;
using static Zebra_RFID_Scanner_Android.Models.File;
using static Zebra_RFID_Scanner_Android.Models.PKL;


namespace Zebra_RFID_Scanner_Android.ViewModels
{
    [QueryProperty(nameof(Url), nameof(Url))]
    [QueryProperty(nameof(TypeStatus), nameof(TypeStatus))]
    [QueryProperty(nameof(PO), nameof(PO))]
    public class ExcelDetailViewModel : BaseViewModel, INotifyPropertyChanged
    {
        private const string ERROL_QTY = "-1";
        private const string EXTENSIONXLSX = ".xlsx";
        private const string EXTENSIONCSV = ".csv";
        private readonly PKLApi _pklApi;
        public Command SaveDataClicked { get; }
        private readonly IAuthenticationService _authenticationService;
        private readonly Services.ISite _site;
        //a
        private static TagItem _mySelectedItem;
        private static Dictionary<String, int> tagListDict = new Dictionary<string, int>();//khai báo khóa-giá trị TagID-sl
        private DateTime startime;
        private int totalTagCount = 0;
        private static string _uniquetags, _totaltags, _totaltime;
        private string _connectionStatus, _readerStatus;
        private System.Timers.Timer aTimer;
        private bool _listAvailable;
        private bool _listCsv;
        private bool _listExcel;
        //a
        private IHostData _hostData;
        private IModalPage modalPage;
        private string _po = "";
        public string Po { get => _po; set { _po = value; OnPropertyChanged(); } }
        private string _so = "";
        public string So { get => _so; set { _so = value; OnPropertyChanged(); } }
        private string _sku = "";
        public string Sku { get => _sku; set { _sku = value; OnPropertyChanged(); } }
        private string _consignee = "";
        public string Consignee { get => _consignee; set { _consignee = value; OnPropertyChanged(); } }
        private string _shipper = "";
        public string Shipper { get => _shipper; set { _shipper = value; OnPropertyChanged(); } }

        private static ObservableCollection<TagItem> _allItems;

        public new event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<File.FileFormat> _excelRow;
        public ObservableCollection<File.FileFormat> ExcelRow
        {
            get { return _excelRow; }
            set
            {
                _excelRow = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<File.FileFormat> _excelRowShow;
        public ObservableCollection<File.FileFormat> ExcelRowShow
        {
            get { return _excelRowShow; }
            set
            {
                _excelRowShow = value;
                OnPropertyChanged(nameof(ExcelRowShow));
            }
        }
        public ObservableCollection<File.EpcReport> EpcReports { get; set; } = new ObservableCollection<File.EpcReport>();
        public ObservableCollection<File.EPCDiscrepancys> ePCDiscrepancys { get; set; } = new ObservableCollection<File.EPCDiscrepancys>();
        public ObservableCollection<TagItem> AllItems { get => _allItems; set => _allItems = value; }


        public TagItem MySelectedItem { get => _mySelectedItem; set => _mySelectedItem = value; }

        public static String SelectedItem
        {
            get { return _mySelectedItem?.InvID; }
        }
        public string UniqueTags { get => _uniquetags; set { _uniquetags = value; OnPropertyChanged(); } }
        public string TotalTags { get => _totaltags; set { _totaltags = value; OnPropertyChanged(); } }
        public string TotalTime { get => _totaltime; set { _totaltime = value; OnPropertyChanged(); } }
        public string readerConnection { get => _connectionStatus; set { _connectionStatus = value; OnPropertyChanged(); } }
        public bool listAvailable { get => _listAvailable; set { _listAvailable = value; OnPropertyChanged(); } }
        public bool hintAvailable { get => !_listAvailable; set { OnPropertyChanged(); } }
        public bool listCsv { get => !_listCsv; set { _listCsv = value; OnPropertyChanged(); } }
        public bool listExcel { get => !_listExcel; set { _listExcel = value; OnPropertyChanged(); } }
        public string readerStatus { get => _readerStatus; set { _readerStatus = value; OnPropertyChanged(); } }

        private Object tagreadlock = new object();
        public DateTime Startime
        {
            get => startime;
            set
            {
                startime = value;
                OnPropertyChanged(nameof(Startime));
            }
        }
        private bool typeStatuts;
        public bool TypeStatus
        {
            get => typeStatuts;
            set
            {
                typeStatuts = value;
                OnPropertyChanged(nameof(TypeStatus));
            }
        }
        private string po;
        public string PO
        {
            get => po;
            set
            {
                po = value;
                OnPropertyChanged(nameof(po));
            }
        }
        private string url;
        public string Url
        {
            get => url;
            set
            {
                url = value;
                OnPropertyChanged(nameof(Url));
            }
        }
        private string text;
        public string Text
        {
            get => text;
            set
            {
                text = value;
                OnPropertyChanged(nameof(Text));
            }
        }
        private string lc;
        public string LC
        {
            get => lc;
            set
            {
                lc = value;
                OnPropertyChanged(nameof(LC));
            }
        }
        public ExcelDetailViewModel()
        {
            _authenticationService = DependencyService.Get<IAuthenticationService>();
            _site = DependencyService.Get<Services.ISite>();
            if (_authenticationService == null || string.IsNullOrEmpty(_authenticationService.SessionId))
            {
                Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
            if (_allItems == null)
            {
                _allItems = new ObservableCollection<TagItem>();
            }
            else
            {
                _allItems.Clear();
                EpcReports.Clear();
                tagListDict.Clear();
            }
            UniqueTags = "0";
            TotalTags = "0";
            TotalTime = DateTime.ParseExact("00:00:00", "HH:mm:ss", CultureInfo.InvariantCulture).ToString("HH\\:mm\\:ss");
            ExcelRowShow = new ObservableCollection<File.FileFormat>();
          
            // UI for hint
            updateHints();
            Title = Url;
            Text = Url;
            _pklApi = new PKLApi(_authenticationService);
            SaveDataClicked = new Command(OnSaveDataClicked);
            // collection
        }

        // Xử lý khi đọc được thẻ RFID, lưu trữ và hiển thị thông tin thẻ.
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void TagReadEvent(TagData[] aryTags)
        {
            try
            {
                lock (tagreadlock)
                {
                    for (int index = 0; index < aryTags.Length; index++)
                    {
                        String tagID = aryTags[index].TagID;
                        if (tagID.Length >= 24)
                        {
                            if (tagID != null)
                            {
                                if (tagListDict.ContainsKey(tagID))
                                {
                                    tagListDict[tagID] = tagListDict[tagID] + aryTags[index].TagSeenCount;
                                    UpdateCount(tagID, tagListDict[tagID], aryTags[index].PeakRSSI);
                                }
                                else
                                {
                                    tagListDict.Add(tagID, aryTags[index].TagSeenCount);
                                    UpdateList(tagID, aryTags[index].TagSeenCount, aryTags[index].PeakRSSI);
                                    try
                                    {
                                        UpdateExcel(tagID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
                                        break;
                                    }
                                }
                            }
                            totalTagCount++;
                            updateCounts();
                            _listAvailable = true;
                            OnPropertyChanged(nameof(_listAvailable));
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }
        }
        private void UpdateExcel(string epc)
        {
            try
            {
                string upc = EpctoUpc.EpctoUpcs(epc);
                File.FileFormat checkFileFormat;
                File.EPCDiscrepancys checkEPCDiscrepancys = null;
                lock (tagreadlock) // Sử dụng lock để đảm bảo truy cập an toàn vào checkFileFormat
                {
                    try
                    {

                        checkEPCDiscrepancys = ePCDiscrepancys?.FirstOrDefault(x => x.Id == epc);
                        if (checkEPCDiscrepancys != null)
                        {
                            checkFileFormat = ExcelRowShow?.FirstOrDefault(x => x.CartonTo.Trim() == checkEPCDiscrepancys.Carton.Trim() && x.PO.Trim() == checkEPCDiscrepancys.Po.Trim());
                        }
                        else
                        {
                            checkFileFormat = null;
                        }

                    }
                    catch (Exception ex)
                    {
                        checkFileFormat = null;
                        Log.Error("UpdateExcel", $"Error updating Excel for tagID '{epc}': {ex.Message}");
                    }
                }
                if (checkFileFormat != null)
                {
                    checkFileFormat.Location = LC;
                    checkFileFormat.qtyscan = (int.Parse(checkFileFormat.qtyscan) + 1).ToString();
                    if (checkFileFormat.Qty != ERROL_QTY)
                    {
                        if (int.Parse(checkFileFormat.Qty) > int.Parse(checkFileFormat.qtyscan))
                        {
                            checkFileFormat.Color = System.Drawing.Color.Red;
                        }
                        else if (checkFileFormat.Qty == checkFileFormat.qtyscan)
                        {
                            checkFileFormat.Color = System.Drawing.Color.Green;
                            checkFileFormat.Status = true;
                        }
                        else
                        {
                            checkFileFormat.Color = System.Drawing.Color.Yellow;
                            checkFileFormat.Status = false;
                        }

                        ePCDiscrepancys.Remove(checkEPCDiscrepancys);
                        EpcReports.Add(new File.EpcReport
                        {
                            EPC = epc,
                            IdReports = Url.Replace(EXTENSIONXLSX, "").Replace(EXTENSIONCSV, ""),
                            Po = checkEPCDiscrepancys.Po,
                            SKU = checkEPCDiscrepancys.SKU,
                            So = checkEPCDiscrepancys.So,
                            UPC = checkEPCDiscrepancys.UPC,
                            CartonTo = checkEPCDiscrepancys.Carton,
                            cntry = checkEPCDiscrepancys.cntry,
                            port = checkEPCDiscrepancys.port,
                            deviceNum = rfidModel.rfidReader.HostName.ToString(),
                            doNo = checkEPCDiscrepancys.doNo,
                            setCd = checkEPCDiscrepancys.setCd,
                            subDoNo = checkEPCDiscrepancys.subDoNo,
                            mngFctryCd = checkEPCDiscrepancys.mngFctryCd,
                            facBranchCd = checkEPCDiscrepancys.facBranchCd,
                            packKey = checkEPCDiscrepancys.packKey,
                        });

                    }
                }
                else
                {
                    try
                    {
                        ExcelRowShow.Add(new File.FileFormat
                        {
                            CartonTo = "",
                            PO = epc,
                            Qty = "-1",
                            qtyscan = "1",
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error("UpdateExcel", $"Error updating Excel for tagID '{epc}': {ex.Message}");
                        Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
                    }
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }
        }

        private File.FileFormat GetFileFormat(string upc)
        {
            try
            {
                var checkFileFormat = ExcelRowShow.FirstOrDefault(x => x.UPC == upc);
                if (checkFileFormat == null)
                {
                    return null;
                }
                else
                {
                    return checkFileFormat;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private void UpdateList(String tag, int count, short rssi)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                _allItems.Add(new TagItem { InvID = tag, TagCount = count, RSSI = rssi });
            });
        }

        private void UpdateCount(String tag, int count, short rssi)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var found = _allItems.FirstOrDefault(x => x.InvID == tag);
                if (found != null)
                {
                    found.TagCount = count;
                    found.RSSI = rssi;
                }
            });
        }
        // Xử lý khi nhấn giữ nút trigger, bắt đầu/dừng quá trình đọc thẻ.
        public override void HHTriggerEvent(bool pressed)
        {
            if (pressed)
            {
                PerformInventory();
                listAvailable = true;
                hintAvailable = false;
            }
            else
            {
                StopInventory();
            }
        }
        //Dừng quá trình đọc thẻ RFID
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void StopInventory()
        {
            rfidModel.StopInventory();
            aTimer?.Stop();
            aTimer?.Dispose();
        }
        //Bắt đầu quá trình đọc thẻ RFID.
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void PerformInventory()
        {
            //Device.BeginInvokeOnMainThread(() => { tagListDict.Clear(); _allItems.Clear(); });
            //totalTagCount = 0;

            SetTimer();
            rfidModel.PerformInventory();
        }
        //Xử lý các sự kiện trạng thái của đầu đọc RFID.
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void StatusEvent(Events.StatusEventData statusEvent)
        {
            if (statusEvent.StatusEventType == STATUS_EVENT_TYPE.InventoryStartEvent)
            {
                //startime = DateTime.Now;
            }
            if (statusEvent.StatusEventType == STATUS_EVENT_TYPE.InventoryStopEvent)
            {
                updateCounts();
                int total = 0;
                foreach (var entry in tagListDict)
                    total += entry.Value;
                Console.WriteLine("Unique tags " + tagListDict.Count + " Total tags" + total);
            }
        }
        //Cập nhật các thuộc tính TotalTags, UniqueTags, TotalTime.
        private void updateCounts()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                UniqueTags = tagListDict.Count.ToString();
                TotalTags = totalTagCount.ToString();

            });
        }
        //Thiết lập bộ đếm thời gian để cập nhật UI định kỳ.
        private void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        //Xử lý sự kiện khi bộ đếm thời gian hết hạn.
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            updateCounts();
            Device.BeginInvokeOnMainThread(() =>
            {
                TimeSpan span = TimeSpan.Parse(TotalTime);
                TotalTime = span.Add(TimeSpan.FromSeconds(1)).ToString();
            });
        }
        //Xử lý khi đầu đọc RFID kết nối/ngắt kết nối.
        public override void ReaderConnectionEvent(bool connection)
        {
            base.ReaderConnectionEvent(connection);
            updateHints();
            aTimer?.Stop();
            aTimer?.Dispose();
        }
        //Cập nhật các gợi ý và thông tin trên UI.
        private void updateHints()
        {
            if (_allItems.Count == 0)
            {
                _listAvailable = false;
                readerConnection = isConnected ? "Connected" : "Not connected";
                if (isConnected)
                {
                    readerStatus = rfidModel.isBatchMode ? "Inventory is running in batch mode" : "Press and hold the trigger for tag reading";
                }
            }
            else
            {
                _listAvailable = true;
            }

        }

        //Đọc file Excel từ URL và lưu trữ dữ liệu vào ObservableCollection.
        public async Task ReadEx(ObservableCollection<File.FileFormat> cartonRows, ObservableCollection<File.EPCDiscrepancys> EPCDiscrepancys, ObservableCollection<string> epcs)
        {
            try
            {
                UserDialogs.Instance.ShowLoading("Loading...");
                Location location = await GetCurrentLocationAsync();
                LC = location.Latitude + ", " + location.Longitude;
                _hostData = DependencyService.Get<IHostData>();
                modalPage = DependencyService.Get<IModalPage>();
                modalPage.modalPage = false;
                ExcelRowShow.Clear();
                var client = new HttpClient();
                    listCsv = false;
                    listExcel = true;
                    var distinctSoValues = cartonRows.Select(cr => cr.So).Distinct();
                    var distinctPoValues = cartonRows.Select(cr => cr.PO).Distinct();
                    var distinctSkuValues = cartonRows.Select(cr => cr.Sku).Distinct();
                    So = string.Join("-", distinctSoValues);
                    Po = string.Join("-", distinctPoValues);
                    Sku = string.Join("-", distinctSkuValues);

                    ExcelRow = cartonRows;
                    ExcelRowShow = new ObservableCollection<FileFormat>(cartonRows.Where(x => x.StatusCtn));
                    ePCDiscrepancys = EPCDiscrepancys;
                    Startime = DateTime.Now;
                    foreach(var item in epcs)
                    {
                        if (item != null)
                        {
                            if (tagListDict.ContainsKey(item))
                            {
                                tagListDict[item] = tagListDict[item] + 1;
                                UpdateCount(item, tagListDict[item], 1);
                            }
                            else
                            {
                                tagListDict.Add(item, 0);
                                UpdateList(item, 1, 0);
                                try
                                {
                                    UpdateExcel(item);
                                    EpcReports.Remove(EpcReports.SingleOrDefault(x => x.EPC == item));
                                }
                                catch (Exception ex)
                                {
                                    Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
                                    break;
                                }
                            }
                        }
                        totalTagCount++;
                        updateCounts();
                        _listAvailable = true;
                        OnPropertyChanged(nameof(_listAvailable));
                       
                    }
            }
            catch (System.Exception ex)
            {
                throw new Exception("An error occurred: " + ex.Message);
            }
            finally
            {
                UserDialogs.Instance.HideLoading();
            }
        }



        private async Task CreateListXLSX(string CartonToE, string POE, string SkuE, string SoE, string upcE, string qtyE)
        {
            try
            {
                var checkUpc = ExcelRow?.FirstOrDefault(x => x.UPC == upcE);
                if (!So.Contains(SoE)) So += SoE + "-";
                if (!Po.Contains(POE)) Po += POE + "-";
                if (!Sku.Contains(SkuE)) Sku += SkuE + "-";

                if (checkUpc == null)
                {

                    ExcelRow.Add(new File.FileFormat
                    {
                        CartonTo = CartonToE,
                        PO = POE,
                        Sku = SkuE,
                        So = SoE,
                        UPC = upcE,
                        Qty = qtyE,
                        qtyscan = "0",
                        Status = false,
                    });

                }
                else
                {
                    try
                    {
                        checkUpc.Qty = (int.Parse(checkUpc.Qty) + int.Parse(qtyE)).ToString();
                    }
                    catch (Exception ex)
                    {
                        Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Toast.MakeText(Android.App.Application.Context, "Failed: Value " + qtyE + " illegal.", ToastLength.Short).Show();
                            Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
                        });
                        throw new Exception("Failed: Value " + qtyE + " illegal.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed :" + ex.Message);
            }

        }

        private async void OnSaveDataClicked()
        {
            try
            {
                UserDialogs.Instance.ShowLoading("Loading...");
                _hostData = DependencyService.Get<IHostData>();
                string apiMaersk = !typeStatuts ? $"{_hostData.HostDatas}/Home/Save" : $"{_hostData.HostDatas}/Home/EditAll";
                string apiClient = $"{_hostData.HostDatas}/ClientHandheld/Save";
                string apiRequest = _site.Sites == "client" ? apiClient : apiMaersk;
                var id = Url.Replace(EXTENSIONXLSX, "").Replace(EXTENSIONCSV, "");
                string TimeStart = Startime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                ExcelRowShow = new ObservableCollection<File.FileFormat>(ExcelRowShow.Where(x => int.Parse(x.Qty) >= 0).ToList());
                var ctnErrors = new ObservableCollection<File.FileFormat>(ExcelRow
                                .Where(row => !ExcelRowShow.Any(show => show.CartonTo == row.CartonTo))
                               .ToList());
                var responseObject = JsonConvert.DeserializeObject<YourResponseObject>(await _pklApi.SaveDataAsync(apiRequest, typeStatuts, id, ExcelRowShow, ctnErrors, EpcReports, Po.TrimEnd('-'), So.TrimEnd('-'), Sku.TrimEnd('-'), "[]", Consignee, Shipper, TimeStart, ePCDiscrepancys,LC,_site.Sites));
                if (responseObject.Code is string)
                {
                    string codeAsString = (string)responseObject.Code;
                    Toast.MakeText(Android.App.Application.Context, responseObject.msg, ToastLength.Short).Show();
                }
                else if (IsNumeric(responseObject.Code.ToString()))
                {
                    int codeAsInt = Convert.ToInt32(responseObject.Code);
                    if (codeAsInt == 200)
                    {
                        Toast.MakeText(Android.App.Application.Context, responseObject.msg, ToastLength.Short).Show();
                        await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
                    }
                    else
                    {
                        Toast.MakeText(Android.App.Application.Context, responseObject.msg, ToastLength.Short).Show();
                    }
                }

            }
            catch (Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }
            finally { UserDialogs.Instance.HideLoading(); }
        }
        public async Task<Location> GetCurrentLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    return location;
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Xử lý khi tính năng không được hỗ trợ trên thiết bị
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Xử lý khi tính năng chưa được bật
            }
            catch (PermissionException pEx)
            {
                // Xử lý khi không có quyền truy cập vị trí
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
            }

            return null;
        }
        //Thông báo cho UI về sự thay đổi của thuộc tính.
        protected new virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // Phương thức kiểm tra số
        private bool IsNumeric(string value)
        {
            return double.TryParse(value, out _);
        }
    }
}

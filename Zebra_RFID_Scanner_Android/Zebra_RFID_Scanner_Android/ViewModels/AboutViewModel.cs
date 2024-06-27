using Acr.UserDialogs;
using Android.Media.TV;
using Android.Widget;
using Java.Net;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Zebra_RFID_Scanner_Android.Api;
using Zebra_RFID_Scanner_Android.Droid;
using Zebra_RFID_Scanner_Android.Services;
using Zebra_RFID_Scanner_Android.Views;
using static Android.Resource;
using static Java.Util.Jar.Attributes;
using static Zebra_RFID_Scanner_Android.Models.File;
using static Zebra_RFID_Scanner_Android.Models.PKL;
namespace Zebra_RFID_Scanner_Android.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        private readonly PKLApi _pklApi;
        private readonly IAuthenticationService _authenticationService;
        private readonly IHostData hostData;
        private IModalPage modalPage;
        private ObservableCollection<string> pathList;
        private ObservableCollection<string> pathListScanned;

        public ICommand SearchCommand { get; private set; }
        public ICommand Refresh { get; private set; }
        public new event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> PathList
        {
            get { return pathList; }
            set
            {
                pathList = value;
                OnPropertyChanged(nameof(PathList));
            }
        }
        public ObservableCollection<string> PathListScanned
        {
            get { return pathListScanned; }
            set
            {
                pathListScanned = value;
                OnPropertyChanged(nameof(PathListScanned));
            }
        }
        private string host;
        public string Host
        {
            get { return host; }
            set
            {
                host = value;
                OnPropertyChanged(nameof(Host));
            }
        }
        public string Search { get; set; }
        //private int _currentTabIndex;

        //public int CurrentTabIndex
        //{
        //    get { return _currentTabIndex; }
        //    set
        //    {
        //        if (_currentTabIndex != value)
        //        {
        //            _currentTabIndex = value;
        //            OnPropertyChanged(nameof(CurrentTabIndex));
        //        }
        //    }
        //}
        public AboutViewModel()
        {
            Title =  "List PKL";
            _authenticationService = DependencyService.Get<IAuthenticationService>();
            hostData = DependencyService.Get<IHostData>();
            Host = hostData?.HostDatas.Trim()+ "/FunctionOrder/List?seach=";
            if (_authenticationService == null || string.IsNullOrEmpty(_authenticationService.SessionId))
            {
                Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
            _pklApi = new PKLApi(_authenticationService);
            SearchCommand = new Command(OnSearchCommand);
            Refresh = new Command(async () => await RefreshData());
            PathList = new ObservableCollection<string>();
            PathListScanned = new ObservableCollection<string>();
        }
        private async Task RefreshData()
        {
            await LoadFileList();
        }
        public async Task LoadFileList()
        {
            try
            {
                UserDialogs.Instance.ShowLoading("Loading...");
                modalPage = DependencyService.Get<IModalPage>();
                modalPage.modalPage = false;
                var responseObject = JsonConvert.DeserializeObject<YourResponseObject>(await _pklApi.CallApi(Host));
                var responseObjectScanned = JsonConvert.DeserializeObject<YourResponseObject>(await _pklApi.CallApi(hostData?.HostDatas.Trim() + "/Reports/UnconfirmedReports?name=&date="));

                PathList.Clear();
                PathListScanned.Clear();
                foreach (var item in responseObject.Pkl)
                {
                    PathList.Add(item.Name);
                }
                foreach (var item in responseObjectScanned.reports)
                {
                    var checkPort = await _pklApi.CallApi(hostData?.HostDatas.Trim() + "/Reports/checkPort?id=" + item.id);
                    if (string.IsNullOrEmpty(checkPort))
                    {
                        PathListScanned.Add(item.id + ".xlsx");
                    }
                    else
                    {
                        PathListScanned.Add(item.id + ".csv");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Path"))
                {
                    Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
                }

            }
            finally
            {
                UserDialogs.Instance.HideLoading();
            }
        }
        private async void OnSearchCommand()
        {
            try
            {
                UserDialogs.Instance.ShowLoading("Loading...");
                string apiRequest = Host + Search;
                var responseObject = JsonConvert.DeserializeObject<YourResponseObject>(await _pklApi.CallApi(apiRequest));
                var responseObjectScanned = JsonConvert.DeserializeObject<YourResponseObject>(await _pklApi.CallApi(hostData?.HostDatas.Trim() + "/Reports/UnconfirmedReportsToSearch?search="+Search));

                if (responseObject != null)
                {
                    // Clear và cập nhật PathList trên luồng UI
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        PathList.Clear();
                        PathListScanned.Clear();
                        foreach (var item in responseObject.Pkl)
                        {
                            PathList.Add(item.Name);
                        }
                        foreach (var item in responseObjectScanned.reports)
                        {
                            var checkPort = await _pklApi.CallApi(hostData?.HostDatas.Trim() + "/Reports/checkPort?id=" + item.id);
                            if (string.IsNullOrEmpty(checkPort))
                            {
                                PathListScanned.Add(item.id+".xlsx");
                            }
                            else
                            {
                                PathListScanned.Add(item.id + ".csv");
                            }
                        }
                    });

                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Error due to list display", "OK");
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi một cách graceful
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred while processing your request." + ex.Message, "OK");
            }finally
            {
                UserDialogs.Instance.HideLoading();
            }
        }
        public async Task OnListItemClicked(string url)
        {
            try
            {
                if (url == null)
                    return;
                var type = url.Contains(".xlsx")?false : true;
                var pathApi = $"{hostData?.HostDatas.ToString()}/Home/CheckPreASN?name={url.Replace(".xlsx", "").Replace(".csv", "")}&type={type}";
                var responseObject = JsonConvert.DeserializeObject<YourResponseObject>(await _pklApi.CallApi(pathApi));
                if (responseObject.Code is string)
                {
                    string codeAsString = (string)responseObject.Code;
                    //Toast.MakeText(Android.App.Application.Context, responseObject.msg, ToastLength.Short).Show();
                    var confirmConfig = new ConfirmConfig
                    {
                        Message = "Already in the system, continue scanning?",
                        OkText = "Continue",
                        CancelText = "Cancel"
                    };

                    var result = await UserDialogs.Instance.ConfirmAsync(confirmConfig);

                    if (result)
                    {
                        await Shell.Current.GoToAsync($"{nameof(ExcelDetailPage)}?{nameof(ExcelDetailViewModel.Url)}={url}&{nameof(ExcelDetailViewModel.TypeStatus)}={true}");
                    }
                    else
                    {
                        // Người dùng đã chọn hủy bỏ hoặc đóng hộp thoại
                        // Thực hiện các hành động cần thiết ở đây
                    }
                }
                else if (IsNumeric(responseObject.Code.ToString()))
                {
                    int codeAsInt = Convert.ToInt32(responseObject.Code);
                    if (codeAsInt == 200)
                    {

                        // This will push the ItemDetailPage onto the navigation stack
                        await Shell.Current.GoToAsync($"{nameof(ExcelDetailPage)}?{nameof(ExcelDetailViewModel.Url)}={url}&{nameof(ExcelDetailViewModel.TypeStatus)}={false}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred while processing your request." + ex.Message, "OK");
            }
        }
        protected new virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private bool IsNumeric(string value)
        {
            return double.TryParse(value, out _);
        }
    }
}
using Java.IO;
using Java.Sql;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;
using static Zebra_RFID_Scanner_Android.Models.PKL;
using Zebra_RFID_Scanner_Android.Api;
using Zebra_RFID_Scanner_Android.Droid;
using Android.Widget;
using Zebra_RFID_Scanner_Android.Services;
using Java.Net;
using Android.Views;
using Acr.UserDialogs;
using System.Linq;
using Zebra_RFID_Scanner_Android.Views;
using System.Threading.Tasks;

namespace Zebra_RFID_Scanner_Android.ViewModels
{
    public class GetFileViewModel : BaseViewModel
    {
        private readonly PKLApi _pklApi;
        private readonly IHostData hostData;
        private readonly IAuthenticationService _authenticationService;
        private AboutViewModel aboutViewModel;
        public ICommand GetFileClicked {get; private set;}

        public new event PropertyChangedEventHandler PropertyChanged;

        private string portcode;
        public string PortCode
        {
            get { return portcode; }
            set
            {
                portcode = value;
                OnPropertyChanged();
            }
        }
        private DateTime date;

        public DateTime Date
        {
            get { return date; }
            set
            {
                date = value;
                OnPropertyChanged();
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
        public GetFileViewModel()
        {
            Title = "GetFile";
            Date = DateTime.Today;
            hostData = DependencyService.Get<IHostData>();
            _authenticationService = DependencyService.Get<IAuthenticationService>();
            _pklApi = new PKLApi(_authenticationService);
            aboutViewModel = new AboutViewModel();
            Host = hostData?.HostDatas.Trim();
            GetFileClicked = new Command(OnGetFileClicked);
        }
        
        private async void OnGetFileClicked()
        {
            try
            {
                UserDialogs.Instance.ShowLoading("Loading...");
                string ApiUrl = Host + "/functionorder/GetFileApi";
                string date = Date.ToString("yyyy-MM-dd");
                string portcode = PortCode?.ToString()??"";
                var responseObject = JsonConvert.DeserializeObject<Result>(await _pklApi?.GetFileApi(ApiUrl, portcode, date));
                if(responseObject.Code == 200)
                {
                    // Hiển thị hộp thoại danh sách để lựa chọn
                    // Lọc danh sách để loại bỏ các phần tử có giá trị trống
                    var filteredList = responseObject.PO
                        .Where(item => !string.IsNullOrEmpty(item))
                        .ToList();
                    var actionSheetConfig = new ActionSheetConfig()
                        .SetTitle("Select PO to continue scanning")
                        .SetCancel("Cancel");
                    // Thêm các lựa chọn vào hộp thoại
                    foreach (var item in filteredList) // Thay YourList bằng tên danh sách trong đối tượng phản hồi
                    {
                        actionSheetConfig.Add(item, async () => await OnOptionSelected(item, portcode+"_"+date, responseObject.TypeStatus)); // Thay OnOptionSelected bằng phương thức xử lý lựa chọn của bạn
                    }

                    UserDialogs.Instance.ActionSheet(actionSheetConfig);
                }
                else
                {
                    Toast.MakeText(Android.App.Application.Context, responseObject.msg, ToastLength.Short).Show();
                }
            }
            catch(Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }
            finally
            {
                UserDialogs.Instance?.HideLoading();
            }
        }
        public async Task OnOptionSelected(string item,string id,bool typeStatus)
        {
            try
            {
                await Shell.Current.GoToAsync($"{nameof(ExcelDetailPage)}?{nameof(ExcelDetailViewModel.Url)}={id}&{nameof(ExcelDetailViewModel.TypeStatus)}={typeStatus}&{nameof(ExcelDetailViewModel.PO)}={item}");

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
    }
}

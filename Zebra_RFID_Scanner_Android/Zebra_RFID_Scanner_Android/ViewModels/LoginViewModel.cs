using Acr.UserDialogs;
using Android;
using Android.Widget;
using Java.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Zebra_RFID_Scanner_Android.Droid;
using Zebra_RFID_Scanner_Android.Services;
using Zebra_RFID_Scanner_Android.Views;
using static Zebra_RFID_Scanner_Android.Models.LoginReponse;


namespace Zebra_RFID_Scanner_Android.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string username;
        private string password;
        private IAuthenticationService _authenticationService;
        private Services.ISite _site;
        private IHostData _hostData;
       
        public string Username
        {
            get { return username; }
            set
            {
                username = value;
                OnPropertyChanged();
            }
        }
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                OnPropertyChanged();
            }
        }

        private bool rememberUsername;

        public bool RememberUsername
        {
            get { return rememberUsername; }
            set
            {
                rememberUsername = value;
                OnPropertyChanged();
            }
        }
        public ICommand LoginCommand { get; private set; }
        public ICommand ForgotPasswordCommand { get; private set; }
        public new event PropertyChangedEventHandler PropertyChanged;
        public LoginViewModel()
        {
            LoginCommand = new Command(OnLoginClicked);
            ForgotPasswordCommand = new Command(OnForgotPasswordCommand);
        }

        private async void OnForgotPasswordCommand()
        {
            // Hiển thị modal thông báo ở đây
            await Application.Current.MainPage.DisplayAlert("Forgot Password", "Please contact the administrator !!!", "OK");
        }

        private async void OnLoginClicked()
        {
            try
            {
                UserDialogs.Instance.ShowLoading("Loading...");
                _authenticationService = DependencyService.Get<IAuthenticationService>();
                _site = DependencyService.Get<Services.ISite>();
                _hostData = DependencyService.Get<IHostData>();
                using (HttpClient client = new HttpClient())
                {
                    string apiUrl = $"{_hostData.HostDatas.Trim()}/login/loginigms?user={username}&pass={Password}";

                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    if (response != null && response.Headers != null)
                    {
                        response.EnsureSuccessStatusCode();
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        var responseObject = JsonConvert.DeserializeObject<Reponse>(jsonContent);
                        string sessionID = "";
                        if (responseObject.code == 200)
                        {
                            // Lấy cookie ASP.NET_SessionId từ phản hồi
                            var cookies = response.Headers.GetValues("Set-Cookie");

                            foreach (var cookie in cookies)
                            {
                                if (cookie.Contains("ASP.NET_SessionId"))
                                {
                                    sessionID = cookie.Split(';')[0];
                                    break;
                                }
                            }
                            _authenticationService.SessionId = sessionID.Replace("ASP.NET_SessionId=", "");
                            if (Shell.Current != null)
                            {
                                Preferences.Set("isLoggedIn", true);
                                _site.Sites = responseObject.site;
                                await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
                            }
                            else
                            {
                                Toast.MakeText(Android.App.Application.Context, "Shell.Current is null", ToastLength.Short).Show();
                            }
                        }
                        else
                        {
                            Toast.MakeText(Android.App.Application.Context, responseObject.msg, ToastLength.Short).Show();
                        }
                    }
                    else
                    {
                        Toast.MakeText(Android.App.Application.Context, "Faild", ToastLength.Short).Show();
                    }
                   
                  
                }
            }
            catch(Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }finally
            {
                UserDialogs.Instance.HideLoading();
            }
           
        }
        protected new virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        const int RequestLocationId = 0;
         readonly string[] LocationPermissions =
            {
                Manifest.Permission.AccessCoarseLocation,
                Manifest.Permission.AccessFineLocation
            };
      
        //
      
    }
}

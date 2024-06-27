using Android;
using Android.Content;
using Android.Content.Res;
using Microsoft.Extensions.Configuration;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Zebra_RFID_Scanner_Android.Droid;
using Zebra_RFID_Scanner_Android.Services;
using Zebra_RFID_Scanner_Android.ViewModels;
using Zebra_RFID_Scanner_Android.Views;

namespace Zebra_RFID_Scanner_Android
{
    public partial class App : Application
    {
        public static IAuthenticationService AuthenticationService { get; } = new AuthenticationService();
        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            DependencyService.Register<AuthenticationService>();
            MainPage = new AppShell();
            //MainPage.BindingContext = new LoginViewModel();
        }
        protected override async void OnStart()
        {

            if (Preferences.Get("isLoggedIn", true))
            {
                await Shell.Current.GoToAsync($"//{nameof(GetFilePage)}");
            }
            else
            {
                await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
        }
        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}

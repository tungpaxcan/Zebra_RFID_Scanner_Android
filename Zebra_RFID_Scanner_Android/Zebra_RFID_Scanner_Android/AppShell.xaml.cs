using System;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;
using Zebra_RFID_Scanner_Android.ViewModels;
using Zebra_RFID_Scanner_Android.Views;

namespace Zebra_RFID_Scanner_Android
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(ExcelDetailPage), typeof(ExcelDetailPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
            Routing.RegisterRoute(nameof(GetFilePage), typeof(GetFilePage));

        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            Preferences.Set("isLoggedIn", false);
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private void OnGetFileClick(object sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = false;
            Navigation.PushAsync(new GetFilePage());
        }
    }
}

using Android.Content;
using Android.Widget;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Zebra_RFID_Scanner_Android.ViewModels;

namespace Zebra_RFID_Scanner_Android.Views
{
    public partial class AboutPage : TabbedPage
    {

        public AboutPage()
        {
            try
            {
                InitializeComponent();

                BindingContext = new AboutViewModel();
            }
            catch (Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }
        }
        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                if (BindingContext is AboutViewModel viewModel)
                {
                    await viewModel.LoadFileList();  
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }
        }
        private async void OnListItemTapped(object sender, ItemTappedEventArgs e)
        {
            try
            {
                if (e.Item != null)
                {
                    // Lấy tên của hàng được click
                    string url = e.Item.ToString();

                    // Gọi phương thức xử lý sự kiện khi click vào một mục trong ListView từ ViewModel
                    await ((AboutViewModel)BindingContext).OnListItemClicked(url);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }
          
        }
        private void TabbedPage_CurrentPageChanged(object sender, EventArgs e)
        {
            var tabbedPage = sender as TabbedPage;
            var viewModel = tabbedPage?.BindingContext as AboutViewModel;

            if (viewModel != null)
            {
                //viewModel.CurrentTabIndex = tabbedPage.Children.IndexOf(tabbedPage.CurrentPage);
                viewModel.Title = tabbedPage.Children.IndexOf(tabbedPage.CurrentPage) == 0 ? "List PKL" : "Scanned List";
            }
        }
    }
}
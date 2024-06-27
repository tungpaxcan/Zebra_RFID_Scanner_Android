using Android.Media;
using Android.Widget;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

using Zebra_RFID_Scanner_Android.Models;
using Zebra_RFID_Scanner_Android.Services;
using Zebra_RFID_Scanner_Android.Views;
using File = System.IO.File;

namespace Zebra_RFID_Scanner_Android.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {
        private Item _selectedItem;

        public ObservableCollection<Item> Items { get; }
        public Command LoadItemsCommand { get; }
        public Command AddItemCommand { get; }
        public ICommand Aa { get; private set; }
        public Command<Item> ItemTapped { get; }

        public ItemsViewModel()
        {
            Title = "Browse";
            Items = new ObservableCollection<Item>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            ItemTapped = new Command<Item>(OnItemSelected);
            Aa = new Command(OnA);
            AddItemCommand = new Command(OnAddItem);

        }

        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await DataStore.GetItemsAsync(true);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void OnAppearing()
        {
            IsBusy = true;
            SelectedItem = null;
        }

        public Item SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnItemSelected(value);
            }
        }

        private async void OnAddItem(object obj)
        {
            await Shell.Current.GoToAsync(nameof(NewItemPage));
        }
        private async void OnA()
        {
            try
            {
                var assetManager = Android.App.Application.Context.Assets;
                string fileName = "SCMP0003.pfx";
                // Đường dẫn đến thư mục Download trên thiết bị
                using(System.IO.Stream stream = assetManager.Open("SCMP0003.pfx"))
                {
                    var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    var pfxByteArray = ms.ToArray();
                    // Đọc nội dung của tệp chứng chỉ
                    //byte[] certBytes = File.ReadAllBytes(downloadFolderPath);
                    //var cert = new X509Certificate2(downloadFolderPath, "tO8RQjNU");
                    //var handler = new HttpClientHandler();
                    ////handler.ClientCertificates.Add(cert);
                    //var client = new HttpClient(handler);
                    var client = new AndroidHttpsClientHandler(pfxByteArray);
                    var httpClient = new HttpClient(client);
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("https://stage-spl.fastretailing.com//tps/presign?action=download&port_code=VNSGN&date=2024-01-13"),
                        Method = HttpMethod.Get,
                    };
                    var response = await httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseContent);
                        Toast.MakeText(Android.App.Application.Context, responseContent, ToastLength.Short).Show();
                    }
                }

                
                }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình gọi API
                Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
            }
        }
        private async Task<byte[]> DownloadCertificateAsync()
        {
            // Thay đổi URL này để tải xuống chứng chỉ của bạn
            var certificateUrl = "https://your-certificate-url.com/certificate.crt";

            var client = new HttpClient();
            var response = await client.GetAsync(certificateUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                throw new Exception("Lỗi khi tải xuống chứng chỉ: " + response.StatusCode);
            }
        }
        private async void A(object obj)
        {
            await Shell.Current.GoToAsync(nameof(NewItemPage));
        }
        async void OnItemSelected(Item item)
        {
            if (item == null)
                return;

            // This will push the ItemDetailPage onto the navigation stack
            await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.Id}");
        }
    }
}
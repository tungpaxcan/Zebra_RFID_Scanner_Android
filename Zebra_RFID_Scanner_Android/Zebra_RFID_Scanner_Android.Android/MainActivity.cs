using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Android.Content;
using Zebra_RFID_Scanner_Android.ViewModels;
using Xamarin.Forms;
using Zebra_RFID_Scanner_Android.Services;
using AndroidX.Lifecycle;
using Acr.UserDialogs;
using Android.Widget;
using Zebra_RFID_Scanner_Android.Views;
using WebSocket4Net;
using System.Text;

namespace Zebra_RFID_Scanner_Android.Droid
{

    //[Activity(Label = "MainActivity", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    [Activity(Label = "MainActivity", Theme = "@style/MainTheme", MainLauncher = true, LaunchMode = Android.Content.PM.LaunchMode.SingleTop)]
    [IntentFilter(new[] { "com.darryncampbell.datawedge.xamarin.ACTION" }, Categories = new[] { Intent.CategoryDefault })]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private ModalCheckCartonViewModel _viewModel;
        private IHostData host;
        private IModalPage modalPage;
        private WebSocket _webSocket;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            UserDialogs.Init(this);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            DependencyService.Register<IHostData, HostData>();
            DependencyService.Register<IIntentService, ModalCheckCartonViewModel>();
            DependencyService.Register<IModalPage,ModalPage>();
            var prefs = GetSharedPreferences("MyPrefs", FileCreationMode.Private);
            bool isConfigured = prefs.GetBoolean("isConfigured", false);
            string hostData = prefs.GetString("HostData", null);
           
            if (!isConfigured)
            {
                // Nếu chưa cấu hình xong, chuyển sang CustomInstallerActivity
                var customInstallerIntent = new Intent(this, typeof(CustomInstallerActivity));
                StartActivity(customInstallerIntent);
                Finish(); // Kết thúc MainActivity để không quay lại được
            }
            else
            {
                // Nếu đã cấu hình xong, làm bất cứ điều gì bạn cần với dữ liệu hostData
                // Ví dụ: truyền dữ liệu này đến ViewModel hoặc xử lý nó trực tiếp trong MainActivity
                if (!string.IsNullOrEmpty(hostData))
                {
                    host = DependencyService.Get<IHostData>();
                    host.HostDatas = hostData;
                }
            }
            LoadApplication(new App());
          
         
            _viewModel = ViewModelLocator.ModalCheckCartonViewModel;
            DWUtilities.CreateDWProfile(this);
            // Khởi tạo và kết nối WebSocket
            InitializeWebSocket();
        }
        private void InitializeWebSocket()
        {
            _webSocket = new WebSocket("ws://192.168.1.102/ws");

            _webSocket.Opened += (sender, e) => Console.WriteLine("Opened connection ád");
            _webSocket.DataReceived += (sender, e) =>
            {
                // Chuyển đổi mảng byte thành chuỗi
                string receivedData = Encoding.UTF8.GetString(e.Data);
                Console.WriteLine("Received data ad: " + receivedData);
            };
            _webSocket.MessageReceived += (sender, e) => Console.WriteLine("Received ád: " + e.Message );
            _webSocket.Error += (sender, e) => Console.WriteLine("Error áda: " + e.Exception.Message);
            _webSocket.Closed += (sender, e) => Console.WriteLine("### closed ád ###");

            _webSocket.Open();
        }
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            modalPage = DependencyService.Get<IModalPage>();
            if (modalPage.modalPage)
            {
                DisplayScanResult(intent);
            }
        }
        private void DisplayScanResult(Intent scanIntent)
        {
            string decodedData = scanIntent.GetStringExtra(Resources.GetString(Resource.String.datawedge_intent_key_data));
            if (!string.IsNullOrEmpty(decodedData))
            {
                _viewModel.ProcessIntentData(decodedData);
            }
            Console.WriteLine(decodedData);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            // Đóng kết nối WebSocket khi Activity bị hủy
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                _webSocket.Close();
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


    }
}
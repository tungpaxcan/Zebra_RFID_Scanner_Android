using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Lifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Xamarin.Essentials.Platform;
using Intent = Android.Content.Intent;

namespace Zebra_RFID_Scanner_Android.Droid
{
    //[Activity(Label = "CustomInstallerActivity")]
    [Activity(Label = "Zebra_RFID_Scanner_Android", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class CustomInstallerActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Kiểm tra xem đã cấu hình xong chưa
            bool isConfigured = GetSharedPreferences("MyPrefs", FileCreationMode.Private).GetBoolean("isConfigured", false);
            if (!isConfigured)
            {
                // Thiết lập giao diện cho activity
                SetContentView(Resource.Layout.custom_installer_layout);

                // Tìm ô nhập dữ liệu trong layout
                EditText editText = FindViewById<EditText>(Resource.Id.editText);
                // Xử lý sự kiện khi nhấn nút "Hoàn tất"
                Button buttonFinish = FindViewById<Button>(Resource.Id.buttonFinish);
                buttonFinish.Click += (sender, e) =>
                {
                    // Lấy dữ liệu từ ô nhập
                    string userInput = editText.Text;
                    // Lưu dữ liệu vào SharedPreferences
                    var prefs = GetSharedPreferences("MyPrefs", FileCreationMode.Private);
                    var editor = prefs.Edit();
                    editor.PutBoolean("isConfigured", true);
                    editor.PutString("HostData", userInput);
                    editor.Apply();
                    var mainActivityIntent = new Intent(this, typeof(MainActivity));
                    StartActivity(mainActivityIntent);
                    // Đóng activity
                    Finish();
                };
             
            }
            else
            {
                // Nếu đã cấu hình xong, chuyển hướng sang MainActivity
                var mainActivityIntent = new Intent(this, typeof(MainActivity));
                StartActivity(mainActivityIntent);
                Finish(); // Kết thúc CustomInstallerActivity để không quay lại được
            }
          
        }
    }
}
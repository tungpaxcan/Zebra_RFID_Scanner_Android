using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Zebra_RFID_Scanner_Android.ViewModels;

namespace Zebra_RFID_Scanner_Android.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GetFilePage : ContentPage
    {
        
        public GetFilePage()
        {
            InitializeComponent();
            BindingContext = new GetFileViewModel();
        }
        protected override async void OnAppearing()
        {
                base.OnAppearing();
        }
    }
}
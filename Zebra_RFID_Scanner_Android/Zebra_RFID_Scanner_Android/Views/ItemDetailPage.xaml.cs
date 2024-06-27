using System.ComponentModel;
using Xamarin.Forms;
using Zebra_RFID_Scanner_Android.ViewModels;

namespace Zebra_RFID_Scanner_Android.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}
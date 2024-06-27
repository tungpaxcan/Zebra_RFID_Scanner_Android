using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Zebra_RFID_Scanner_Android.Models;
using Zebra_RFID_Scanner_Android.ViewModels;

namespace Zebra_RFID_Scanner_Android.Views
{
    public partial class NewItemPage : ContentPage
    {
        public Item Item { get; set; }

        public NewItemPage()
        {
            InitializeComponent();
            BindingContext = new NewItemViewModel();
        }
    }
}
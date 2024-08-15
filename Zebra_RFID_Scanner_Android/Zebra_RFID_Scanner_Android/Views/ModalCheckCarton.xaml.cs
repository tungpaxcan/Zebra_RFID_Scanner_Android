using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Zebra_RFID_Scanner_Android.ViewModels;
using Android.Widget;
using Zebra_RFID_Scanner_Android.Services;
using System.Collections.ObjectModel;
using Zebra_RFID_Scanner_Android.Models;
using System.Windows.Input;


namespace Zebra_RFID_Scanner_Android.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ModalCheckCarton : ContentPage
    {
        private ModalCheckCartonViewModel viewmodel;
        public event EventHandler OkClicked;
        private string url = "";
        public string Url { get => url; set { url = value; OnPropertyChanged(); } }
        private string po = "";
        public string PO { get => po; set { po = value; OnPropertyChanged(); } }
        private bool typeStatuts = false;
        public bool TypeStatus { get => typeStatuts; set { typeStatuts = value; OnPropertyChanged(); } }

       
        public ModalCheckCarton(string url,string po,bool typeStatuts)
        {
            InitializeComponent();
            Url = url;
            TypeStatus = typeStatuts;
            PO = po;
            BindingContext = viewmodel = ViewModelLocator.ModalCheckCartonViewModel;

           
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ModalCheckCartonViewModel viewModel)
            {
                try
                {
                    await viewModel.ReadEx(Url,PO, TypeStatus);
                    viewModel.UpdateIn();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(Android.App.Application.Context, ex.Message, ToastLength.Short).Show();
                }
            }
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is ModalCheckCartonViewModel viewModel)
            {
                viewModel.UpdateOut();
            }

        }
        private void OnOkButtonClicked(object sender, EventArgs e)
        {
            OkClicked?.Invoke(this, EventArgs.Empty);
            Navigation.PopModalAsync();
        }

        public ObservableCollection<File.FileFormat> GetCartonRows()
        {
            return new ObservableCollection<File.FileFormat>(viewmodel.CartonRows);
        }
        public ObservableCollection<File.EPCDiscrepancys> GetEPCDiscrepancys()
        {
            return viewmodel.ePCDiscrepancys;
        }
        public ObservableCollection<string> GetEPCs()
        {
            return viewmodel.epcs;
        }
    }
}
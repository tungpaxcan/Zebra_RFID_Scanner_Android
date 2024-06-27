using Android.Widget;
using Java.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Zebra_RFID_Scanner_Android.ViewModels;
using static Android.App.Assist.AssistStructure;

namespace Zebra_RFID_Scanner_Android.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ExcelDetailPage : ContentPage
    {
        private ExcelDetailViewModel viewmodel;
        private bool isFistOpenModal = false;
        public bool IsFistOpenModal { get => isFistOpenModal; set { isFistOpenModal = value; OnPropertyChanged(); } }
        public ExcelDetailPage()
        {
            InitializeComponent();
            BindingContext = viewmodel = new ExcelDetailViewModel();            
        }
        private async Task ShowModalCheckCartonAsync(string url,string po, bool typeStatuts)
        {
            var modalPage = new ModalCheckCarton(url,po,typeStatuts);
            var tcs = new TaskCompletionSource<bool>();
            modalPage.OkClicked += (s, e) =>
            {
                tcs.SetResult(true);
            };

            await Navigation.PushModalAsync(modalPage);
            IsFistOpenModal = true;
            await tcs.Task;

            // Chỉ chạy khi nút OK được nhấn
            var excelRow = modalPage.GetCartonRows();
            var EPCDiscrepancys = modalPage.GetEPCDiscrepancys();
            var epcs = modalPage.GetEPCs();
            await viewmodel.ReadEx(excelRow, EPCDiscrepancys, epcs);
            viewmodel.UpdateIn();

            // Buộc cập nhật giao diện
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ExcelDetailViewModel viewModel)
            {
                try
                {
                    if (!IsFistOpenModal)
                    {
                        await ShowModalCheckCartonAsync(viewModel.Url, viewmodel.PO,viewModel.TypeStatus);
                    }
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
            if (BindingContext is ExcelDetailViewModel viewModel)
            {
                viewModel.UpdateOut();
            }
              
        }

    }
}
using System;
using System.Collections.Generic;
using System.Text;
using Zebra_RFID_Scanner_Android.ViewModels;

namespace Zebra_RFID_Scanner_Android.Services
{
    public class ViewModelLocator
    {
        static ModalCheckCartonViewModel _modalCheckCartonViewModel;

        public static ModalCheckCartonViewModel ModalCheckCartonViewModel
            => _modalCheckCartonViewModel ??= new ModalCheckCartonViewModel();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Zebra_RFID_Scanner_Android.Services
{
    public interface IDWUtilitie
    {
        void enableScanner(Android.Content.Context context);
        void disableScanner(Android.Content.Context context);
    }
}

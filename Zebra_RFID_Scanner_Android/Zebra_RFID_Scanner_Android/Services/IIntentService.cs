using System;
using System.Collections.Generic;
using System.Text;

namespace Zebra_RFID_Scanner_Android.Services
{

    public interface IIntentService
    {
        void ProcessIntentData(string data);
    }

}

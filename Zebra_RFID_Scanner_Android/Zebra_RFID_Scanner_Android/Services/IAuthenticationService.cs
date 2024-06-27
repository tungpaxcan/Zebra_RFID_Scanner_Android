using System;
using System.Collections.Generic;
using System.Text;

namespace Zebra_RFID_Scanner_Android.Services
{

        public interface IAuthenticationService
        {
            string SessionId { get; set; }
        }

}

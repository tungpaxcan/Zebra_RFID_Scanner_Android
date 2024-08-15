using System;
using System.Collections.Generic;
using System.Text;

namespace Zebra_RFID_Scanner_Android.Models
{
    internal class LoginReponse
    {
        public class Reponse
        {
            public int code { get; set; }
            public string Url { get; set; }
            public string msg { get; set; }
            public string site { get; set; }
        }
    }
}

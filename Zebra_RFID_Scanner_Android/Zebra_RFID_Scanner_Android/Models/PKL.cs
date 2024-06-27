using System;
using System.Collections.Generic;
using System.Text;

namespace Zebra_RFID_Scanner_Android.Models
{
    internal class PKL
    {
        public class PklItem
        {
            public string Path { get; set; }
            public string Name { get; set; }
        }
        public class Reports
        {
            public string createBy { get; set; }
            public string createDate { get; set; }
            public string id { get; set; }
        }
        public class Result
        {
            public int Code { get; set; }
            public string msg { get; set; }
            public string fileUrl { get; set; }
            public List<string> PO { get; set; }
            public bool TypeStatus { get; set; }
        }
        public class YourResponseObject
        {
            public object Code { get; set; }
            public List<PklItem> Pkl { get; set; }
            public List<Reports> reports { get; set; }
            public List<string> Rsl { get; set; }
            public string msg { get; set; }
        }
    }
}

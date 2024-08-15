using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace Zebra_RFID_Scanner_Android.Models
{
    public class File
    {
        public class FileFormat : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public string CartonTo { get; set; }
            public string IdReports { get; set; }
            public string PO { get; set; }
            public string Sku { get; set; }
            public string cntry { get; set; }
            public string port { get; set; }
            public string deviceNum { get; set; }
            public string deviceNumClient { get; set; }
            public string doNo { get; set; }
            public string setCd { get; set; }
            public string subDoNo { get; set; }
            public string packKey { get; set; }
            public string mngFctryCd { get; set; }
            public string facBranchCd { get; set; }
            public string So { get; set; }
            private bool statusCtn;

            public bool StatusCtn
            {
                get { return statusCtn; }
                set
                {
                    if (statusCtn != value)
                    {
                        statusCtn = value;
                        OnPropertyChanged(nameof(StatusCtn));
                    }
                }
            }
            private bool status;

            public bool Status
            {
                get { return status; }
                set
                {
                    if (status != value)
                    {
                        status = value;
                        OnPropertyChanged(nameof(Status));
                    }
                }
            }
            private string qty;

            public string UPC { get; set; }

            public string Qty
            {
                get { return qty; }
                set
                {
                    if (qty != value)
                    {
                        qty = value;
                        OnPropertyChanged(nameof(Qty));
                    }
                }
            }
            private string Qtyscan;
            public string qtyscan
            {
                get { return Qtyscan; }
                set
                {
                    if (Qtyscan != value)
                    {
                        Qtyscan = value;
                        OnPropertyChanged(nameof(qtyscan));
                    }
                }
            }
            private string location;
            public string Location
            {
                get { return location; }
                set
                {
                    if (location != value)
                    {
                        location = value;
                        OnPropertyChanged(nameof(Location));
                    }
                }
            }
            public Color color;
            public Color Color {
                get { return color; }
                set
                {
                    if (color != value)
                    {
                        color = value;
                        OnPropertyChanged(nameof(Color));
                    }
                }
            }
            public Color colorCtn;
            public Color ColorCtn
            {
                get { return colorCtn; }
                set
                {
                    if (colorCtn != value)
                    {
                        colorCtn = value;
                        OnPropertyChanged(nameof(ColorCtn));
                    }
                }
            }
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public class EpcReport : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public string EPC { get; set; }
            public string IdReports { get; set; }
            public string Po { get; set; }
            public string SKU { get; set; }
            public string So { get; set; }
            public string UPC { get; set; }
            public string CartonTo { get; set; }
            public string cntry { get; set; }
            public string port { get; set; }
            public string deviceNum { get; set; }
            public string deviceNumClient { get; set; }
            public string subDoNo { get; set; }
            public string doNo { get; set; }
            public string setCd { get; set; }
            public string packKey { get; set; }
            public string mngFctryCd { get; set; }
            public string facBranchCd { get; set; }
        }
        public class EPCDiscrepancys : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public string Consignee { get; set; }
            public string Shipper { get; set; }
            public string Id { get; set; }
            public string IdReports { get; set; }
            public string Po { get; set; }
            public string SKU { get; set; }
            public string So { get; set; }
            public string UPC { get; set; }
            public string cntry { get; set; }
            public string port { get; set; }
            public string doNo { get; set; }
            public string setCd { get; set; }
            public string packKey { get; set; }
            public string Carton { get; set; }
            public string deviceNum { get; set; }
            public string subDoNo { get; set; }
            public string mngFctryCd { get; set; }
            public string facBranchCd { get; set; }
            public string location { get; set; }
            public bool statusClient { get; set; }
        }
        public class InfoCtn
        {
            public int code { get; set; }
            public string createDate { get; set; }
            public string createBy { get; set; }
            public List<Ctn> Ctn { get; set; }
            public string po { get; set; }
            public string so { get; set; }
            public string sku { get; set; }
            public string modifyDate { get; set; }
            public string Consignee { get; set; }
            public string Shipper { get; set; }
            public int TotalPages { get; set; }
            public int pages { get; set; }
            public string msg { get; set; }
            public List<Epc> epcToUpc { get; set; }
        }
        public class Ctn
        {
            public string so { get; set; }
            public string po { get; set; }
            public string sku { get; set; }
            public int id { get; set; }
            public string ctn { get; set; }
            public string status { get; set; }
            public bool statusClient { get; set; }
            public string upc { get; set; }
            public string qty { get; set; }
        }
        public class Epc
        {
            public string so { get; set; }
            public string po { get; set; }
            public string sku { get; set; }
            public string upc { get; set; }
            public string ctn { get; set; }
            public bool status { get; set; }
            public string EPC { get; set; }
            public string cntry { get; set; }
            public string dptPortCd { get; set; }
            public string doNo { get; set; }
            public string setCd { get; set; }
            public string subDoNo { get; set; }
            public string mngFctryCd { get; set; }
            public string facBranchCd { get; set; }
            public string packKey { get; set; }
            public bool statusClient { get; set; }
        }
    }
}

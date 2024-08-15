using Android.Opengl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Com.Zebra.Rfid.Api3;
using Zebra_RFID_Scanner_Android.Models;
using Zebra_RFID_Scanner_Android.Services;

namespace Zebra_RFID_Scanner_Android.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public static ReaderModel rfidModel = ReaderModel.readerModel;
        private static ScannerModel scannerModel = ScannerModel.scannerModel;
        public BaseViewModel()
        {

        }
        public virtual void HHTriggerEvent(bool pressed)
        {

        }

        public virtual void TagReadEvent(TagData[] tags)
        {

        }

        public virtual void StatusEvent(Events.StatusEventData statusEvent)
        {

        }
        public virtual void ReaderConnectionEvent(bool connection)
        {
            isConnected = connection;
        }

        public virtual void ReaderAppearanceEvent(bool appeared)
        {

        }


        internal void UpdateIn()
        {
            rfidModel.TagRead += TagReadEvent;
            rfidModel.TriggerEvent += HHTriggerEvent;
            rfidModel.StatusEvent += StatusEvent;
            rfidModel.ReaderConnectionEvent += ReaderConnectionEvent;
            rfidModel.ReaderAppearanceEvent += ReaderAppearanceEvent;
        }

        internal void UpdateOut()
        {
            rfidModel.TagRead -= TagReadEvent;
            rfidModel.TriggerEvent -= HHTriggerEvent;
            rfidModel.StatusEvent -= StatusEvent;
            rfidModel.ReaderConnectionEvent -= ReaderConnectionEvent;
            rfidModel.ReaderAppearanceEvent -= ReaderAppearanceEvent;
        }

        public bool isConnected { get => rfidModel.isConnected; set => OnPropertyChanged(); }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public virtual void ScannerConnectionEvent(string deviceName)
        {

        }

        public virtual void CurrentProgressUpdate(int currentProgress)
        {

        }

        public virtual void FWVersion(string scannerFWVersion)
        {

        }

        internal void UpdateScannerIn()
        {
            scannerModel.ScannerConnectionEvent += ScannerConnectionEvent;
            scannerModel.CurrentProgress += CurrentProgressUpdate;
            scannerModel.FWVersion += FWVersion;
        }

        internal void UpdateScannerOut()
        {
            scannerModel.ScannerConnectionEvent -= ScannerConnectionEvent;
            scannerModel.CurrentProgress -= CurrentProgressUpdate;
            scannerModel.FWVersion -= FWVersion;
        }
        public bool scannerConnected { get => scannerModel.IsConnected; set => OnPropertyChanged(); }
        public string devicName { get => scannerModel.DeviceName; set => OnPropertyChanged(); }
        public string sFWVersion { get => scannerModel.getFWVersion; set => OnPropertyChanged(); }

        public IDataStore<Item> DataStore => DependencyService.Get<IDataStore<Item>>();

        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }
        string textCol1 = string.Empty;
        public string TextCol1
        {
            get { return textCol1; }
            set { SetProperty(ref textCol1, value); }
        }
        string textCol2 = string.Empty;
        public string TextCol2
        {
            get { return textCol2; }
            set { SetProperty(ref textCol2, value); }
        }
        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChangeds(propertyName);
            return true;
        }

        #region INotifyPropertyChanged

        protected void OnPropertyChangeds([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

using Android.Mtp;
using Android.Widget;
using Com.Zebra.Rfid.Api3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Xamarin.Forms;

namespace Zebra_RFID_Scanner_Android.Models
{
    public class ReaderModel : Java.Lang.Object, Readers.IRFIDReaderEventHandler, IRfidEventsListener, IWifiScanDataEventsListener
    {
        private static ReaderModel _ReaderModel;

        public IList<ReaderDevice> ReadersList = new List<ReaderDevice>();
        public RFIDReader rfidReader;
        private Readers readers;
        ReaderDevice readerDevice;
        public bool isBatchMode = false;

        private ReaderModel()
        {
            Readers.Attach(this);
            Setup();
        }


        /// <summary>
        /// Connnect with reader after instance creation
        /// </summary>
        public void Setup()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                GetAvailableReaders();
                ConnectReader(LastConnectedReaderIndex);
            });
        }

        #region Events

        /// <summary>
        /// Delegate for tag read event
        /// </summary>
        /// <param name="tags">array of tag data</param>
        internal delegate void TagReadHandler(TagData[] tags);
        internal delegate void TriggerHandler(bool pressed);
        internal delegate void StatusHandler(Events.StatusEventData statusEvent);
        internal delegate void ConnectionHandler(bool connected);
        internal delegate void ReaderAppearanceHandler(bool appeared);
        internal delegate void WiFiNotification(string scanStatus);
        internal delegate void WiFiScanResults(WifiScanData scanData);

        internal event TagReadHandler TagRead;
        internal event TriggerHandler TriggerEvent;
        internal event StatusHandler StatusEvent;
        internal event ConnectionHandler ReaderConnectionEvent;
        internal event ReaderAppearanceHandler ReaderAppearanceEvent;
        internal event WiFiNotification WiFiNotificationEvent;
        internal event WiFiScanResults WiFiScanResultsEvent;

        #endregion

        public Boolean isConnected
        {
            get => getConnectionStatus();
        }

        private bool getConnectionStatus()
        {
            if (rfidReader != null)
            {
                try
                {
                    return rfidReader.IsConnected;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        public static ReaderModel readerModel
        {
            get
            {
                if (_ReaderModel == null)
                    _ReaderModel = new ReaderModel();
                return _ReaderModel;
            }
        }

        private int LastConnectedReaderIndex
        {
            get
            {
                int index = 0;
                try
                {
                    index = (int)Application.Current?.Properties["ReaderIndex"];
                }
                catch (KeyNotFoundException) { }
                return index;
            }
            set
            {
                Application.Current.Properties["ReaderIndex"] = value;
                Application.Current.SavePropertiesAsync();
            }
        }


        /// <summary>
        /// Set trigger mode to rfid on resume / screen appearance or start
        /// </summary>
        internal void SetTriggerMode()
        {
            //Try to connect
            try
            {
                if (rfidReader != null && !rfidReader.IsConnected)
                {
                    ConnectReader(LastConnectedReaderIndex);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("RFID Reader Connection error");
            }
            if (isConnected)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    try
                    {
                        rfidReader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.RfidMode, true);
                    }
                    catch (OperationFailureException e)
                    {
                        e.PrintStackTrace();
                    }
                });
            }
        }
        public void RFIDReaderAppeared(ReaderDevice readerDevice)
        {
            ReadersList.Add(readerDevice);
            ReaderAppearanceEvent?.Invoke(true);
        }

        public void RFIDReaderDisappeared(ReaderDevice readerDevice)
        {
            ReadersList.Remove(readerDevice);
            ReaderAppearanceEvent?.Invoke(false);
        }

        public IList<ReaderDevice> GetAvailableReaders()
        {
            bool serialDeviceNotFound = false;
            ReadersList.Clear();
            // For MC33XX and RFD2000
            try
            {
                if (readers == null)
                    readers = new Readers(Android.App.Application.Context, ENUM_TRANSPORT.ServiceSerial);
                ReadersList = readers.AvailableRFIDReaderList;
            }
            catch (Exception)
            {
                serialDeviceNotFound = true;
                readers.Dispose();
                readers = null;
            }

            // For RFD40
            if (ReadersList != null && ReadersList.Count == 0)
            {
                if (readers != null)
                {
                    readers.Dispose();
                    readers = new Readers(Android.App.Application.Context, ENUM_TRANSPORT.ServiceUsb);
                    ReadersList = readers.AvailableRFIDReaderList;
                }


            }
            // For RFD8500
            if (ReadersList != null && ReadersList.Count == 0)
            {
                if (readers != null)
                {
                    readers.Dispose();
                    readers = new Readers(Android.App.Application.Context, ENUM_TRANSPORT.Bluetooth);
                    ReadersList = readers.AvailableRFIDReaderList;
                }


            }

            return ReadersList;
        }

        public void ConnectReader(int index)
        {
            Console.WriteLine("ConnectReader" + index);
            ThreadPool.QueueUserWorkItem(o =>
            {
                ConnectReaderSync(index);
            });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ConnectReaderSync(int index)
        {
            try
            {
                isBatchMode = false;
                Console.WriteLine("Available readers " + ReadersList.Count);
                if (ReadersList.Count > 0 && index < ReadersList.Count)
                {
                    readerDevice = ReadersList[index];
                    rfidReader = readerDevice.RFIDReader;
                    //rfidReader.ReinitTransport();
                    rfidReader.Connect();
                    ConfigureReader();
                    ReaderConnectionEvent?.Invoke(true);
                    Console.WriteLine("Connected " + rfidReader.HostName);
                    LastConnectedReaderIndex = index;

                    //setup Scanner SDK
                    setupScannerSDK();
                }
            }
            catch (InvalidUsageException e)
            {
                e.PrintStackTrace();
            }
            catch (OperationFailureException e)
            {
                ReaderConnectionEvent?.Invoke(false);
                e.PrintStackTrace();
                //ShowAlert("Connection failed\n" + e.StatusDescription);
                ShowAlert("Connection failed\n" + e.Results);
                Console.WriteLine(e.StatusDescription);
                if (e.Results == RFIDResults.RfidBatchmodeInProgress)
                {
                    isBatchMode = true;
                }
            }
        }


        /// <summary>
        /// Configure Reader
        /// Setup event listnere and enable required event types
        /// Set trigger mode to rfid
        /// Configure antenna, singulation and trigger setting using single API call
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ConfigureReader()
        {
            if (rfidReader.IsConnected)
            {
                try
                {
                    // receive events from reader
                    rfidReader.Events.AddEventsListener(this);
                    //ReaderConnection.rfidReader.Events.EventReadNotify += EventReadNotifier;
                    //ReaderConnection.rfidReader.Events.EventStatusNotify += EventStatusNotifier;

                    // HH event
                    rfidReader.Events.SetHandheldEvent(true);

                    // tag event with tag data
                    rfidReader.Events.SetTagReadEvent(true);
                    rfidReader.Events.SetAttachTagDataWithReadEvent(false);
                    //
                    rfidReader.Events.SetInventoryStartEvent(true);
                    rfidReader.Events.SetInventoryStopEvent(true);
                    rfidReader.Events.SetOperationEndSummaryEvent(true);
                    rfidReader.Events.SetReaderDisconnectEvent(true);
                    rfidReader.Events.SetBatteryEvent(true);
                    rfidReader.Events.SetPowerEvent(true);
                    rfidReader.Events.SetTemperatureAlarmEvent(true);
                    rfidReader.Events.SetBufferFullEvent(true);
                    rfidReader.Events.SetBufferFullWarningEvent(true);

                    //WiFi Event
                    rfidReader.Events.AddWifiScanDataEventsListener(this);
                    rfidReader.Events.SetWPAEvent(true);
                    rfidReader.Events.SetScanDataEvent(true);

                    // set trigger mode as rfid, pass second parameter as true so scanner will be disabled
                    rfidReader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.RfidMode, true);

                    // configure for antenna and singulation etc.
                    var antenna = rfidReader.Config.Antennas.GetAntennaRfConfig(1);
                    antenna.SetrfModeTableIndex(0);
                    antenna.TransmitPowerIndex = rfidReader.ReaderCapabilities.GetTransmitPowerLevelValues().Length - 1;
                    rfidReader.Config.Antennas.SetAntennaRfConfig(1, antenna);

                    var singulation = rfidReader.Config.Antennas.GetSingulationControl(1);
                    singulation.Session = SESSION.SessionS0;
                    singulation.Action.InventoryState = INVENTORY_STATE.InventoryStateA;
                    singulation.Action.SetPerformStateAwareSingulationAction(false);
                    rfidReader.Config.Antennas.SetSingulationControl(1, singulation);

                    /*
                    // set start and stop triggers
                    TriggerInfo triggerInfo = new TriggerInfo();
                    triggerInfo.StartTrigger.TriggerType = START_TRIGGER_TYPE.StartTriggerTypeImmediate;
                    triggerInfo.StopTrigger.TriggerType = STOP_TRIGGER_TYPE.StopTriggerTypeImmediate;

                    rfidReader.Config.StartTrigger = triggerInfo.StartTrigger;
                    rfidReader.Config.StopTrigger = triggerInfo.StopTrigger;
                    */

                    TAG_FIELD[] tag_fields = { TAG_FIELD.PeakRssi, TAG_FIELD.TagSeenCount };
                    rfidReader.Config.TagStorageSettings.SetTagFields(tag_fields);

                    // If RFD8500 then disable batch mode and DPO
                    if (rfidReader.ReaderCapabilities.ModelName.Contains("RFD8500"))
                    {
                        rfidReader.Config.SetBatchMode(BATCH_MODE.Disable);
                        // Important: DPO should be disabled based on need here disabled for all operations
                        rfidReader.Config.DPOState = DYNAMIC_POWER_OPTIMIZATION.Disable;
                        //
                        rfidReader.Config.BeeperVolume = BEEPER_VOLUME.HighBeep;
                    }

                    var HostName = rfidReader.HostName;
                    var region = rfidReader.Config.RegulatoryConfig.Region;
                    var modelName = rfidReader.ReaderCapabilities.ModelName;
                    var serialNumber = rfidReader.ReaderCapabilities.SerialNumber;
                    var radioVersion = "";
                    var moduleVersion = "";
                    var deviceVersionInfo = new Android.Runtime.JavaDictionary<string, string>();
                    rfidReader.Config.GetDeviceVersionInfo(deviceVersionInfo);
                    if (deviceVersionInfo.ContainsKey(Constants.Nge))
                    {
                        radioVersion = deviceVersionInfo[Constants.Nge]; //NGE
                    }

                    if (deviceVersionInfo.ContainsKey(Constants.GenxDevice))
                    {
                        moduleVersion = deviceVersionInfo[Constants.GenxDevice]; //RFID_DEVICE
                    }
                    VersionInfo versioninfo = rfidReader.VersionInfo();

                    var message = string.Format("HostName:{0} \n Region:{1} \n ModelName:{2} \n SerialNumber:{3} \n RadioVersion:{4} \n ModuleVersion:{5} \n SDKVersion:{6}",
                                                       HostName, region, modelName, serialNumber, radioVersion, moduleVersion, versioninfo.Version);

                    Console.WriteLine(message);

                    rfidReader.Config.GetDeviceStatus(true, true, true);
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                }
                catch (OperationFailureException e)
                {
                    e.PrintStackTrace();
                    ShowAlert(e);
                }
            }
        }


        internal bool SetKeyLayoutType(string selectedUTrigger, string selectedLTrigger)
        {
            if (rfidReader.IsConnected)
            {
                ENUM_NEW_KEYLAYOUT_TYPE upperTrigger = null, lowerTrigger = null;
                switch (selectedUTrigger)
                {
                    case AppConstants.RFID:
                        upperTrigger = ENUM_NEW_KEYLAYOUT_TYPE.Rfid;
                        break;
                    case AppConstants.SledScanner:
                        upperTrigger = ENUM_NEW_KEYLAYOUT_TYPE.SledScan;
                        break;
                    case AppConstants.TerminalScanner:
                        upperTrigger = ENUM_NEW_KEYLAYOUT_TYPE.TerminalScan;
                        break;
                    case AppConstants.ScanNotification:
                        upperTrigger = ENUM_NEW_KEYLAYOUT_TYPE.ScanNotify;
                        break;
                    case AppConstants.NoAction:
                        upperTrigger = ENUM_NEW_KEYLAYOUT_TYPE.NoAction;
                        break;
                }

                switch (selectedLTrigger)
                {
                    case AppConstants.RFID:
                        lowerTrigger = ENUM_NEW_KEYLAYOUT_TYPE.Rfid;
                        break;
                    case AppConstants.SledScanner:
                        lowerTrigger = ENUM_NEW_KEYLAYOUT_TYPE.SledScan;
                        break;
                    case AppConstants.TerminalScanner:
                        lowerTrigger = ENUM_NEW_KEYLAYOUT_TYPE.TerminalScan;
                        break;
                    case AppConstants.ScanNotification:
                        lowerTrigger = ENUM_NEW_KEYLAYOUT_TYPE.ScanNotify;
                        break;
                    case AppConstants.NoAction:
                        lowerTrigger = ENUM_NEW_KEYLAYOUT_TYPE.NoAction;
                        break;
                }
                RFIDResults rFIDResults = rfidReader.Config.SetKeylayoutType(upperTrigger, lowerTrigger);
                if (rFIDResults == RFIDResults.RfidApiSuccess)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public string GetUpperTrigger()
        {
            if (rfidReader.IsConnected)
            {
                var keylayoutType = rfidReader.Config.UpperTriggerValue;
                if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.Rfid))
                {
                    return AppConstants.RFID;
                }
                else if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.SledScan))
                {
                    return AppConstants.SledScanner;
                }
                else if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.TerminalScan))
                {
                    return AppConstants.TerminalScanner;
                }
                else if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.ScanNotify))
                {
                    return AppConstants.ScanNotification;
                }
                else if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.NoAction))
                {
                    return AppConstants.NoAction;
                }
                else
                {
                    return AppConstants.NoAction;
                }

            }
            return "";
        }

        public string GetLowerTrigger()
        {
            if (rfidReader.IsConnected)
            {
                var keylayoutType = rfidReader.Config.LowerTriggerValue;
                if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.Rfid))
                {
                    return AppConstants.RFID;
                }
                else if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.SledScan))
                {
                    return AppConstants.SledScanner;
                }
                else if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.TerminalScan))
                {
                    return AppConstants.TerminalScanner;
                }
                else if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.ScanNotify))
                {
                    return AppConstants.ScanNotification;
                }
                else if (keylayoutType.Equals(ENUM_NEW_KEYLAYOUT_TYPE.NoAction))
                {
                    return AppConstants.NoAction;
                }
                else
                {
                    return AppConstants.NoAction;
                }

            }
            return "";
        }

        /*
		private void EventStatusNotifier(object sender, EventStatusNotifyEventArgs e)
		{
			
		}

		private void EventReadNotifier(object sender, EventReadNotifyEventArgs e)
		{
			
		}
		*/

        public void EventReadNotify(RfidReadEvents readEvent)
        {
            TagData[] myTags = rfidReader.Actions.GetReadTags(10);
            if (myTags != null)
            {
                ThreadPool.QueueUserWorkItem(o => TagRead?.Invoke(myTags));
            }
        }

        public void EventStatusNotify(RfidStatusEvents rfidStatusEvents)
        {
            Console.WriteLine("Status Notification: " + rfidStatusEvents.StatusEventData.StatusEventType);
            if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.HandheldTriggerEvent)
            {
                if (rfidStatusEvents.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerPressed)
                {
                    ThreadPool.QueueUserWorkItem(o => TriggerEvent?.Invoke(true));
                }
                if (rfidStatusEvents.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerReleased)
                {
                    ThreadPool.QueueUserWorkItem(o => TriggerEvent?.Invoke(false));
                }
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.InventoryStartEvent)
            {
                ThreadPool.QueueUserWorkItem(o => StatusEvent?.Invoke(rfidStatusEvents.StatusEventData));
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.InventoryStopEvent)
            {
                ThreadPool.QueueUserWorkItem(o => StatusEvent?.Invoke(rfidStatusEvents.StatusEventData));
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.OperationEndSummaryEvent)
            {
                int rounds = rfidStatusEvents.StatusEventData.OperationEndSummaryData.TotalRounds;
                int totaltags = rfidStatusEvents.StatusEventData.OperationEndSummaryData.TotalTags;
                long timems = rfidStatusEvents.StatusEventData.OperationEndSummaryData.TotalTimeuS / 1000;
                Console.WriteLine("Summary: Rounds: " + rounds + " Tags: " + totaltags + " Time: " + timems);
                ThreadPool.QueueUserWorkItem(o => StatusEvent?.Invoke(rfidStatusEvents.StatusEventData));
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.DisconnectionEvent)
            {
                ThreadPool.QueueUserWorkItem(o => ReaderConnectionEvent?.Invoke(false));
                ShowAlert("Reader Disconnected");
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.BatteryEvent)
            {
                var battery = rfidStatusEvents.StatusEventData.BatteryData;
                Console.WriteLine("Battery: Cause: " + battery.Cause + " Charging: " + battery.Charging + " Level: " + battery.Level);
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.PowerEvent)
            {
                var power = rfidStatusEvents.StatusEventData.PowerData;
                Console.WriteLine("PowerData: Cause: " + power.Cause + " Current: " + power.Current + " Voltage: " + power.Voltage + " Power " + power.Power);
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.TemperatureAlarmEvent)
            {
                var temperature = rfidStatusEvents.StatusEventData.TemperatureAlarmData;
                Console.WriteLine("TemperatureAlarmEvent: AlarmLevel: " + temperature.AlarmLevel + " AmbientTemp: " + temperature.AmbientTemp + " Current: " + temperature.CurrentTemperature + " PATemp:" + temperature.PATemp);
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.BufferFullWarningEvent)
            {
                Console.WriteLine("BufferFullWarningEvent");
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.BufferFullEvent)
            {
                Console.WriteLine("BufferFullEvent: ");
            }
            else if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.WpaEvent)
            {
                string scanStatus = rfidStatusEvents.StatusEventData.WPAEventData.Type;
                ThreadPool.QueueUserWorkItem(o => WiFiNotificationEvent?.Invoke(scanStatus));
            }
        }

        internal bool PerformInventory()
        {
            try
            {
                rfidReader.ReinitTransport();
                rfidReader.Actions.Inventory.Perform();
                return true;
            }
            catch (InvalidUsageException e)
            {
                e.PrintStackTrace();
            }
            catch (OperationFailureException e)
            {
                e.PrintStackTrace();
                ShowAlert(e);
            }
            return false;
        }

        internal void StopInventory()
        {
            try
            {
                rfidReader.Actions.Inventory.Stop();
            }
            catch (InvalidUsageException e)
            {
                e.PrintStackTrace();
            }
            catch (OperationFailureException e)
            {
                e.PrintStackTrace();
                ShowAlert(e);
            }
        }

        internal void Locate(bool start, string tagPattern, string tagMask)
        {
            try
            {
                if (start)
                {
                    rfidReader.Actions.TagLocationing.Perform(tagPattern, tagMask, null);
                }
                else
                {
                    rfidReader.Actions.TagLocationing.Stop();
                }
            }
            catch (InvalidUsageException e)
            {
                e.PrintStackTrace();
            }
            catch (OperationFailureException e)
            {
                e.PrintStackTrace();
                ShowAlert(e);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Disconnect()
        {
            Console.WriteLine("Disconnect" + rfidReader?.HostName);
            if (rfidReader != null)
            {
                try
                {
                    rfidReader.Disconnect();
                    ReaderConnectionEvent?.Invoke(false);
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                    Console.WriteLine(e.Info);
                }

                //Terminate Scanner SDK
                TerminateScannerSDK();
            }
        }

        public void DeInit()
        {
            Readers.Deattach(this);
            readers?.Dispose();
            readers = null;
        }

        private void ShowAlert(OperationFailureException e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                //Toast.MakeText(Android.App.Application.Context, e.VendorMessage, ToastLength.Short).Show();
            });
            Console.WriteLine(e.VendorMessage);
        }

        public void ShowAlert(string v)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Toast.MakeText(Android.App.Application.Context, v, ToastLength.Short).Show();
            });
            Console.WriteLine(v);
        }

        public bool AddWiFiProfile(string ssid, string password)
        {
            if (rfidReader.IsConnected)
            {
                WifiProfile wifiProfile = new WifiProfile();
                wifiProfile.Setssid(ssid);
                wifiProfile.Setpassword(password);
                try
                {
                    RFIDResults rFIDResults = rfidReader.Config.Wifi_addProfile(wifiProfile);
                    if (rFIDResults == RFIDResults.RfidApiSuccess)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                }
                catch (OperationFailureException e)
                {
                    e.PrintStackTrace();
                }

            }
            return false;
        }

        public bool DeleteWiFiProfile(string ssid)
        {
            if (rfidReader.IsConnected)
            {
                try
                {
                    RFIDResults rFIDResults = rfidReader.Config.Wifi_deleteProfile(ssid);
                    if (rFIDResults == RFIDResults.RfidApiSuccess)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                }
                catch (OperationFailureException e)
                {
                    e.PrintStackTrace();
                }
            }
            return false;
        }

        //public List<string> GetSavedWiFiProfiles()
        //{
        //    List<string> ssidList = new List<string>();
        //    if (rfidReader.IsConnected)
        //    {
        //        try
        //        {
        //            IList<WifiProfile> wifiProfiles = rfidReader.Config.Wifi_listProfile();

        //            if (wifiProfiles != null)
        //            {
        //                foreach (WifiProfile profile in wifiProfiles)
        //                {
        //                    ssidList.Add(profile.Getssid());
        //                }
        //            }

        //        }
        //        catch (InvalidUsageException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //        catch (OperationFailureException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //    }
        //    return ssidList;
        //}

        //public bool ScanWiFi()
        //{
        //    if (rfidReader.IsConnected)
        //    {
        //        try
        //        {
        //            RFIDResults rFIDResults = rfidReader.Config.Wifi_scan();
        //            if (rFIDResults == RFIDResults.RfidApiSuccess)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }

        //        }
        //        catch (InvalidUsageException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //        catch (OperationFailureException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //    }
        //    return false;
        //}

        //public string GetWiFiStatus()
        //{
        //    if (rfidReader.IsConnected)
        //    {
        //        RFIDWifi rfidWifi = new RFIDWifi();
        //        try
        //        {
        //            RFIDResults rFIDResults = rfidReader.Config.Wifi_getStatus(rfidWifi);
        //            if (rFIDResults == RFIDResults.RfidApiSuccess)
        //            {
        //                if (rfidWifi.WifiStatus != null)
        //                {
        //                    if (rfidWifi.WifiStatus.Equals(ENUM_WIFI_STATUS.StatusEnabled))
        //                    {
        //                        return "ENABLED";
        //                    }
        //                    else
        //                    {
        //                        return "DISABLED";
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                return "";
        //            }
        //        }
        //        catch (InvalidUsageException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //        catch (OperationFailureException e)
        //        {
        //            e.PrintStackTrace();
        //        }

        //    }
        //    return "";
        //}

        //public bool WiFiConnect(string ssid)
        //{
        //    if (rfidReader.IsConnected)
        //    {
        //        try
        //        {
        //            RFIDResults rFIDResults = rfidReader.Config.Wifi_connectNonroaming(ssid);
        //            if (rFIDResults == RFIDResults.RfidApiSuccess)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //        catch (InvalidUsageException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //        catch (OperationFailureException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //    }
        //    return false;
        //}

        //public bool WiFiDisconnect()
        //{
        //    if (rfidReader.IsConnected)
        //    {
        //        try
        //        {
        //            RFIDResults rFIDResults = rfidReader.Config.Wifi_disconnect();
        //            if (rFIDResults == RFIDResults.RfidApiSuccess)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //        catch (InvalidUsageException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //        catch (OperationFailureException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //    }
        //    return false;
        //}
        //public bool enableWiFi()
        //{
        //    if (rfidReader.IsConnected)
        //    {
        //        try
        //        {
        //            RFIDResults rFIDResults = rfidReader.Config.Wifi_enable();
        //            if (rFIDResults == RFIDResults.RfidApiSuccess)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //        catch (InvalidUsageException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //        catch (OperationFailureException e)
        //        {
        //            e.PrintStackTrace();
        //        }
        //    }
        //    return false;
        //}

        public void EventWifiScanNotify(RfidWifiScanEvents wifiScanEvent)
        {
            Events.WifiScanEventData data = wifiScanEvent.WifiScanEventData;
            ThreadPool.QueueUserWorkItem(o => WiFiScanResultsEvent?.Invoke(data.Wifiscandata));
        }

        private void setupScannerSDK()
        {
            ScannerModel scannerModel = ScannerModel.scannerModel;
            scannerModel.setupSDKHandler(rfidReader.HostName);
        }

        private void TerminateScannerSDK()
        {
            ScannerModel scannerModel = ScannerModel.scannerModel;
            scannerModel.DisconnectScanner(rfidReader?.HostName);
        }
    }
}

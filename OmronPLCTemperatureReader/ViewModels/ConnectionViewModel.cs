using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Common;
using OmronPLCTemperatureReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class ConnectionViewModel : ViewModelBase
    {
        private Plc plc;
        public ConnectionViewModel(Plc plc)
        {
            plc.ConnectionStatusChanged += Plc_ConnectionStatusChanged;
            this.plc = plc;
            
            if (Port == default(ushort)) Port = 9600;
            //Ip = IPAddress.Parse("192.168.1.130");
            if (Ip == default(IPAddress)) Ip = IPAddress.Parse("192.168.1.51"); //194.187.238.5
            if (Interval == default(int)) Interval = 5;
            ConnectDisconect = new RelayCommand(ConnectDisconectAction, true);
            LoadConnectionSettings("settings.xml");
            OnPropertyChanged("Ip");
        }

        private void Plc_ConnectionStatusChanged(object sender, ConnectionStatusChangedArgs e)
        {

            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("ButtonConnectDisconnectContent");
            OnPropertyChanged("CanEditConnectionSetting");
            //switch (e)
            //{
            //    case ConnectionStatusEnum.CONNECTED:
            //        //TODO
            //        //Czyszczenie serii?
            //        //If series != null, plot.Axes[0].Minimum = plot.Axes[0].DataMinimum?
            //        getValuesTimer.Interval = ConnectionViewModel.Interval * 1000;
            //        getValuesTimer.Enabled = true;
            //        break;
            //    default:
            //        getValuesTimer.Enabled = false;
            //        if (prevReadDMOK)
            //        {
            //            ConnectionRefusedTimes.Add(DateTime.Now); //start
            //            ConnectionRefusedTimes.Add(DateTime.Now); //stop
            //        }
            //        else
            //        {
            //            ConnectionRefusedTimes[ConnectionRefusedTimes.Count - 1] = (DateTime.Now);
            //        }
            //        prevReadDMOK = false;
            //        break;
            //}

        }

        private IPAddress ip;
        public IPAddress Ip
        {
            get { return ip; }
            set
            {
                ip = value;
                SetProperty(ref ip, value);
            }
        }

        private UInt16 port;
        public UInt16 Port
        {
            get { return port; }
            set
            {
                port = value;
                SetProperty(ref port, value);
            }
        }

        private int interval;
        public int Interval
        {
            get { return interval; }
            set
            {
                interval = value;
                SetProperty(ref interval, value);
            }
        }

        public string ButtonConnectDisconnectContent
        {
            get
            {
                switch (plc.ConnectionStatus)
                {
                    case ConnectionStatusEnum.CONNECTED: return "Rozłącz";
                    case ConnectionStatusEnum.CONNECTING:
                    case ConnectionStatusEnum.RECONNECTING: return "Przerwij";
                    default: return "Połącz";
                }
            }
        }
        public bool CanEditConnectionSetting
        {
            get
            {
                switch (plc.ConnectionStatus)
                {
                    case ConnectionStatusEnum.CONNECTED:
                    case ConnectionStatusEnum.CONNECTING:
                    case ConnectionStatusEnum.DISCONNECTING:
                    case ConnectionStatusEnum.RECONNECTING: return false;
                    default: return true;
                }
            }
        }

        public string ConnectionStatus
        {
            get
            {
                switch (plc.ConnectionStatus)
                {
                    case ConnectionStatusEnum.CONNECTED: return "Połączony";
                    case ConnectionStatusEnum.CONNECTING: return "Łączenie...";
                    case ConnectionStatusEnum.CONNECTION_FAILED: return "Połączenie nieudane";
                    case ConnectionStatusEnum.CONNECTION_LOST: return "Połączenie przerwane";
                    case ConnectionStatusEnum.RECONNECTING: return ("Ponowne łączenie... " + plc.AutoReconnectAfterConnectionLostCounter); //+ "/" + plc.AutoReconnectAfterConnectionLostMax; 
                    case ConnectionStatusEnum.DISCONNECTED: return "Rozłączony";
                    case ConnectionStatusEnum.DISCONNECTING: return "Rozłączanie...";
                    default: return "Rozłączony";
                }
            }
        }

        public RelayCommand ConnectDisconect { get; set; }
        CancellationTokenSource connectCancellationTokenSource = new CancellationTokenSource();

        private bool LoadConnectionSettings(string path)
        {

            string loadSettingsLastError;
            try
            {
                XmlDocument settings = new XmlDocument();
                loadSettingsLastError = "Błąd podczas ładowania pliku " + path;
                settings.Load(path);
                loadSettingsLastError = "Brak korzenia w pliku ustawień";
                XmlNode settingsRoot = settings.DocumentElement;
                try { Ip = IPAddress.Parse(settingsRoot.SelectSingleNode("Ip").InnerText); }
                catch { }
                try { Port = ushort.Parse(settingsRoot.SelectSingleNode("Port").InnerText); }
                catch { }
                try { Interval = int.Parse(settingsRoot.SelectSingleNode("Interval").InnerText); }
                catch { }
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void ConnectDisconectAction(object obj)
        {

            if (plc.ConnectionStatus == ConnectionStatusEnum.CONNECTING ||
                plc.ConnectionStatus == ConnectionStatusEnum.RECONNECTING)
            {
                connectCancellationTokenSource.Cancel();
                plc.disconnect();
                connectCancellationTokenSource = new CancellationTokenSource();
            }
            else if (plc.ConnectionStatus == ConnectionStatusEnum.CONNECTED)
            {
                plc.disconnect();
            }
            else if (plc.ConnectionStatus != ConnectionStatusEnum.CONNECTING &&
                     plc.ConnectionStatus != ConnectionStatusEnum.DISCONNECTING &&
                     plc.ConnectionStatus != ConnectionStatusEnum.RECONNECTING)
            {
                Task.Run(new Action(() =>
                {
                    plc.connect(ip, port);
                }), connectCancellationTokenSource.Token);
            }
        }
    }
}

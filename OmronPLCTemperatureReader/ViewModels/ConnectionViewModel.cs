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
        public ConnectionViewModel(ViewModelBase parentViewModel, Plc plc)
        {
            this.ParentViewModel = parentViewModel;
            plc.ConnectionStatusChanged += Plc_ConnectionStatusChanged;
            this.plc = plc;
            
            ConnectDisconect = new RelayCommand(ConnectDisconectAction, true);
        }

        private void Plc_ConnectionStatusChanged(object sender, ConnectionStatusChangedArgs e)
        {

            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("ButtonConnectDisconnectContent");
            OnPropertyChanged("CanEditConnectionSetting");
            OnPropertyChanged("NadsError");
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
                Properties.Settings.Default.IP = value.ToString();
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.Port = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.Interval = value;
                Properties.Settings.Default.Save();
                SetProperty(ref interval, value);
            }
        }

        public string NadsError
        {
            get
            {
                switch (plc.NadsError)
                {
                    case 1:
                        return "The header is not ‘FINS’ (ASCII code)";
                    case 2:
                        return "The data length is too long";
                    case 3:
                        return "The command is not supported";
                    case 32:
                        return "Wszystkie możliwe połączenia są zajęte";
                    case 33:
                        return "The specified node is already connected";
                    case 34:
                        return "Attempt to access a protected node from an unspecified IP address";
                    case 35:
                        return "The client FINS node address is out of range";
                    case 36:
                        return "The same FINS node address is being used by the client and server";
                    case 37:
                        return "All the node addresses available for allocation have been used";
                    default: return null;
                }
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

        private void ConnectDisconectAction(object obj)
        {
            if (plc.ConnectionStatus == ConnectionStatusEnum.CONNECTING ||
                plc.ConnectionStatus == ConnectionStatusEnum.CONNECTED ||
                plc.ConnectionStatus == ConnectionStatusEnum.RECONNECTING)
            {
                plc.Disconnect();
                connectCancellationTokenSource.Cancel();
            }
            else {
                if (plc.ConnectionStatus != ConnectionStatusEnum.CONNECTING &&
                     plc.ConnectionStatus != ConnectionStatusEnum.DISCONNECTING &&
                     plc.ConnectionStatus != ConnectionStatusEnum.RECONNECTING)
                {
                    connectCancellationTokenSource = new CancellationTokenSource();
                    Task.Run(new Action(() =>
                    {
                        plc.Connect(ip, port);
                    }), connectCancellationTokenSource.Token);
                }
            }
        }
    }
}

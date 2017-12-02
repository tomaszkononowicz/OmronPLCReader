using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mcOMRON;
using System.Net;
using System.Windows;
using OmronPLCTemperatureReader.Common;
using System.Timers;

namespace OmronPLCTemperatureReader.Models
{
    public class Plc
    {
        Random random = new Random();
        OmronPLC omronPlc;
        mcOMRON.tcpFINSCommand tcpCommand;
        public event EventHandler<ConnectionStatusEnum> ConnectionStatusChanged;

        private ConnectionStatusEnum connectionStatus;
        public ConnectionStatusEnum ConnectionStatus {
            get { return connectionStatus; }
            private set {
                connectionStatus = value;
                if (ConnectionStatusChanged != null)
                {
                    ConnectionStatusChanged.Invoke(this, value);
                }
            }
        }
        public bool AutoReconnectAfterConnectionLost { get; set; }
        private const int autoReconnectAfterConnectionLostMax = 0;
        public int AutoReconnectAfterConnectionLostMax { get { return autoReconnectAfterConnectionLostMax; } }
        public int AutoReconnectAfterConnectionLostCounter { get; set; }
        private IPAddress ip;
        private ushort port;
        Timer connectionMonitor;
        int licznik = 0;
        object lockerOnlyOneFrameSendInTheSameTime = new object();

        public Plc()
        {
            omronPlc = new OmronPLC(mcOMRON.TransportType.Tcp);
            tcpCommand = ((mcOMRON.tcpFINSCommand)omronPlc.FinsCommand);
            connectionMonitor = new Timer();
            connectionMonitor.Elapsed += ConnectionMonitor_Elapsed; ;
            connectionMonitor.Interval = 2000;
            connectionMonitor.Enabled = false;
            ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;
            AutoReconnectAfterConnectionLost = true;
            AutoReconnectAfterConnectionLostCounter = 0;
            
        }


        private bool tryAndConnect()
        {
            Func<bool> testDataRead = (() =>
            {
                try { if (omronPlc.finsConnectionDataRead(0)) return true; }
                catch (Exception e) { return false; }
                return false;
            });
            Func<bool> testNewConnect = (() =>
            {
                try
                {
                    omronPlc = new OmronPLC(mcOMRON.TransportType.Tcp);
                    tcpCommand = ((mcOMRON.tcpFINSCommand)omronPlc.FinsCommand);
                    tcpCommand.SetTCPParams(ip, port);
                    if (omronPlc.Connect()) {
                        //omronPlc.Close();
                        return true;
                    }                   
                }
                catch (Exception e) { return false; }
                return false;
            });
            if (testDataRead()) return true;
            else if (testNewConnect()) return true;
            return false;

        }


        private void ConnectionMonitor_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (ConnectionStatus != ConnectionStatusEnum.DISCONNECTED)
            {
                if (ConnectionStatus == ConnectionStatusEnum.CONNECTED)
                {
                    lock (lockerOnlyOneFrameSendInTheSameTime)
                    {
                        if (!tryAndConnect())
                        {
                            //Console.WriteLine(omronPlc.LastError);
                            //Console.WriteLine(ConnectionStatusEnum.CONNECTION_LOST);
                            ConnectionStatus = ConnectionStatusEnum.CONNECTION_LOST;
                        }
                    }
                }
                if (ConnectionStatus == ConnectionStatusEnum.CONNECTION_LOST ||
                    ConnectionStatus == ConnectionStatusEnum.RECONNECTING)
                {

                    if (AutoReconnectAfterConnectionLost &&
                        ConnectionStatus != ConnectionStatusEnum.CONNECTED &&
                        ConnectionStatus != ConnectionStatusEnum.CONNECTING &&
                        (AutoReconnectAfterConnectionLostCounter < AutoReconnectAfterConnectionLostMax || AutoReconnectAfterConnectionLostMax == 0))
                    {
                        connect(ip, port, true);
                    }
                    else
                    {
                        ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;
                        //Console.WriteLine(ConnectionStatusEnum.DISCONNECTED);
                    }
                }
            }
        }

        public bool connect(IPAddress ip, ushort port, bool reconnect = false)
        {
            try
            {
                omronPlc.Close();
            }
            catch { }
            this.ip = ip;
            this.port = port;
            if (reconnect)
            {
                ConnectionStatus = ConnectionStatusEnum.RECONNECTING;
                Console.WriteLine(ConnectionStatusEnum.RECONNECTING);
                AutoReconnectAfterConnectionLostCounter++;
            }
            else ConnectionStatus = ConnectionStatusEnum.CONNECTING;
            tcpCommand.SetTCPParams(ip, port);
            if (!tryAndConnect())
            {
                if (ConnectionStatus != ConnectionStatusEnum.DISCONNECTED &&
                    ConnectionStatus != ConnectionStatusEnum.RECONNECTING)
                {
                    ConnectionStatus = ConnectionStatusEnum.CONNECTION_FAILED;
                    MessageBox.Show(omronPlc.LastError);
                    connectionMonitor.Enabled = false;
                }
                return false;
            }
            ConnectionStatus = ConnectionStatusEnum.CONNECTED;
            AutoReconnectAfterConnectionLostCounter = 0;
            connectionMonitor.Enabled = true;
            return true;
        }

        public bool disconnect()
        {
            try
            {
                ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;
                connectionMonitor.Enabled = false;
                omronPlc.Close();      
            }
            catch
            {
                return false;
            }
            return true;
        }



        public int? getValue(ushort dm)
        {
            lock (lockerOnlyOneFrameSendInTheSameTime)
            {
                try
                {
                    omronPlc.Close();
                }
                catch { };
                if (omronPlc.Connect()) {
                    //return random.Next(65535);
                    short result = 0;
                    if (omronPlc.finsConnectionDataRead(0))
                    {
                        if (omronPlc.ReadDM(dm, ref result)) return result;
                    }
                    
                }
                return 0;
            }
        }
    }
}

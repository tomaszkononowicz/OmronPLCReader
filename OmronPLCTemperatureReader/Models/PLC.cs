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
using System.Diagnostics;

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
            Debug.WriteLine(DateTime.Now + " tryAndConnect");
            Func<bool> testDataRead = (() =>
            {
                Debug.WriteLine("  " + DateTime.Now + " testDataRead");
                try {
                    if (omronPlc.finsConnectionDataRead(0))
                        return true;
                }
                catch (Exception e) { return false; }
                return false;
            });
            Func<bool> testNewConnect = (() =>
            {
                Debug.WriteLine("  " + DateTime.Now + " testNewConnect");
                try
                {
                    omronPlc = new OmronPLC(mcOMRON.TransportType.Tcp);
                    tcpCommand = ((mcOMRON.tcpFINSCommand)omronPlc.FinsCommand);
                    tcpCommand.SetTCPParams(ip, port);
                    if (omronPlc.Connect()) { //why exception
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
            Debug.WriteLine(DateTime.Now + " ConnectionMonitor_Elapsed");
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
            Debug.WriteLine(DateTime.Now + " connect");
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
                    //MessageBox.Show(omronPlc.LastError);
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
            Debug.WriteLine(DateTime.Now + " disconnect");
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
                Debug.WriteLine(DateTime.Now + " getValue DM " + dm + " Zaczelo sie");
                try
                {
                    omronPlc.Close();
                }
                catch {
                    Console.WriteLine(omronPlc.LastError);
                };
                short result = 0;
                if (tryAndConnect() && ConnectionStatus == ConnectionStatusEnum.CONNECTED) {
                    omronPlc.ReadDM(dm, ref result);
                    if (tryAndConnect() && ConnectionStatus == ConnectionStatusEnum.CONNECTED)
                    {
                        Debug.WriteLine(DateTime.Now + " getValue DM " + dm + " Skonczylo sie");
                        return result;
                    }
                    }
                Debug.WriteLine(DateTime.Now + " getValue DM " + dm + " Skonczylo sie");
                return null;

            }
        }
    }
}

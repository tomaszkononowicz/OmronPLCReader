using mcOMRON;
using OmronPLCTemperatureReader.Common;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Timers;
using System.Windows;

namespace OmronPLCTemperatureReader.Models
{
    public class Plc
    {
        OmronPLC omronPlc;
        tcpFINSCommand tcpCommand;
        public event EventHandler<ConnectionStatusChangedArgs> ConnectionStatusChanged;

        private ConnectionStatusEnum connectionStatus;
        public ConnectionStatusEnum ConnectionStatus {
            get { return connectionStatus; }
            private set {
                ConnectionStatusEnum prev = connectionStatus;
                connectionStatus = value;
                if (ConnectionStatusChanged != null)
                {
                    ConnectionStatusChanged.Invoke(this, new ConnectionStatusChangedArgs(prev, connectionStatus));
                }
            }
        }
        public bool AutoReconnectAfterConnectionLost { get; set; }
        private const int autoReconnectAfterConnectionLostMax = 0;
        public int AutoReconnectAfterConnectionLostMax { get { return autoReconnectAfterConnectionLostMax; } }
        public int AutoReconnectAfterConnectionLostCounter { get; set; }
        public int NadsError
        {
            get
            {
                if (omronPlc != null && !string.IsNullOrWhiteSpace(omronPlc.LastError))
                {
                    string[] words = omronPlc.LastError.Split(' ');
                    int.TryParse(words[words.Length - 1], out int result);
                    return result;
                }
                else
                {
                    return 0;
                }
            }
        }
        private IPAddress ip;
        private ushort port;
        Timer connectionMonitor;
        object lockerOnlyOneFrameSendInTheSameTime = new object();

        public Plc()
        {
            omronPlc = new OmronPLC(mcOMRON.TransportType.Tcp);
            tcpCommand = (tcpFINSCommand)omronPlc.FinsCommand;
            connectionMonitor = new Timer();
            connectionMonitor.Elapsed += ConnectionMonitor_Elapsed; ;
            connectionMonitor.Interval = 2000;
            connectionMonitor.Enabled = false;
            ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;
            AutoReconnectAfterConnectionLostCounter = 0;
            
        }

        private bool Ping()
        {
            using (Ping ping = new Ping())
            {
                PingReply pingReply = ping.Send(ip, Properties.Settings.Default.PingTimeoutMiliseconds);
                //Console.WriteLine($"[{DateTime.Now}] PingReplyStatus: {pingReply.Status} Time: {pingReply.RoundtripTime}");
                return (pingReply.Status == IPStatus.Success) ? true : false;
            }
        }

        private void ConnectionMonitor_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            if (ConnectionStatus != ConnectionStatusEnum.DISCONNECTED)
            {
                
                if (ConnectionStatus == ConnectionStatusEnum.CONNECTED)
                {
                    bool ping = this.Ping();
                    if (!ping)
                    {
                        ConnectionStatus = ConnectionStatusEnum.CONNECTION_LOST;
                    }
                }
                else
                {
                    if (ConnectionStatus == ConnectionStatusEnum.CONNECTION_LOST || ConnectionStatus == ConnectionStatusEnum.RECONNECTING)
                    {
                        ConnectionStatus = ConnectionStatusEnum.RECONNECTING;
                        AutoReconnectAfterConnectionLostCounter++;
                        Reconnect();
                    }
                }
                
            }
        }

        private void Reconnect()
        {
            bool ping = this.Ping();
            if (ping)
            {
                if (ping)
                {
                    if (omronPlc.Connect())
                    {
                        ConnectionStatus = ConnectionStatusEnum.CONNECTED;
                        AutoReconnectAfterConnectionLostCounter = 0;
                    }
                }
            }
        }

        public bool Connect(IPAddress ip, ushort port)
        {
            if (ConnectionStatus == ConnectionStatusEnum.CONNECTION_FAILED ||
            ConnectionStatus == ConnectionStatusEnum.CONNECTION_LOST ||
            ConnectionStatus == ConnectionStatusEnum.DISCONNECTED ||
            ConnectionStatus == ConnectionStatusEnum.RECONNECTING)
            {
                this.ip = ip;
                this.port = port;
                ConnectionStatus = ConnectionStatusEnum.CONNECTING;
                tcpCommand.SetTCPParams(ip, port);
                bool ping = Ping();
                if (ping)
                {
                    if (omronPlc.Connect())
                    {
                        ConnectionStatus = ConnectionStatusEnum.CONNECTED;
                        AutoReconnectAfterConnectionLostCounter = 0;
                        connectionMonitor.Enabled = true;
                        return true;
                    }
                }
                else
                {
                    MessageBox.Show($"Błąd połączenia: Ping to {ip} failed", "PLCMonitor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                ConnectionStatus = ConnectionStatusEnum.CONNECTION_FAILED;
                connectionMonitor.Enabled = false;
                return false;
            }
            return false;
        }

        public bool Disconnect()
        {
            ConnectionStatus = ConnectionStatusEnum.DISCONNECTING;
            try
            {
                omronPlc.Close();
                connectionMonitor.Enabled = false;
                ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public int? GetValue(ushort dm)
        {
            lock (lockerOnlyOneFrameSendInTheSameTime)
            {
                if (ConnectionStatus == ConnectionStatusEnum.CONNECTED)
                {
                    short result = 0;
                    if (omronPlc.ReadDM(dm, ref result))
                    {
                        return result;
                    }
                }
            }
            return null;
        }
    }
}

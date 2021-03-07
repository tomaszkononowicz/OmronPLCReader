using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class ConnectionTerminalViewModel : ViewModelBase
    {
        public ConnectionTerminalViewModel(ViewModelBase parentViewModel)
        {
            this.ParentViewModel = parentViewModel;

        }

        private IPAddress ip;
        public IPAddress Ip
        {
            get { return ip; }
            set
            {
                ip = value;
                Properties.Settings.Default.TerminalIP = value.ToString();
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
                Properties.Settings.Default.TerminalPort = value;
                Properties.Settings.Default.Save();
                SetProperty(ref port, value);
            }
        }

        private string login;
        public string Login
        {
            get { return login; }
            set
            {
                login = value;
                Properties.Settings.Default.TerminalLogin = value;
                Properties.Settings.Default.Save();
                SetProperty(ref login, value);
            }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                Properties.Settings.Default.TerminalPassword = value;
                Properties.Settings.Default.Save();
                SetProperty(ref password, value);
            }
        }
    }
}


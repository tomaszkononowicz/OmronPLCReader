using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mcOMRON;
using System.Net;
using System.Windows;

namespace OmronPLCTemperatureReader.Models
{
    public class Plc
    {
        Random random = new Random();
        OmronPLC plc;
        mcOMRON.tcpFINSCommand tcpCommand;

        public bool Connected
        {
            get { return plc == null ? false : plc.Connected; }
        }

       public Plc()
        {
            plc = new OmronPLC(mcOMRON.TransportType.Tcp);
            tcpCommand = ((mcOMRON.tcpFINSCommand)plc.FinsCommand);
        }

        public bool connect(IPAddress ip, ushort port)
        {
            tcpCommand.SetTCPParams(ip, port);
            if (!plc.Connect())
            {
                MessageBox.Show(plc.LastError);
                return false;       
            }
            return true;
        }

        public bool disconnect()
        {
            try
            {
                plc.Close();
            } catch
            {
                return false;
            }
            return true;
        }

        public int? getValue(ushort dm)
        {
            short result = 0;
            if (plc.ReadDM(dm, ref result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}

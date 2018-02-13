using OmronPLCTemperatureReader.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmronPLCTemperatureReader.Models
{
    public class ConnectionStatusChangedArgs : EventArgs
    {
        public ConnectionStatusEnum Prev { get; private set; }
        public ConnectionStatusEnum Actual { get; private set; }

        public ConnectionStatusChangedArgs(ConnectionStatusEnum prev, ConnectionStatusEnum actual) : base()
        {
            Prev = prev;
            Actual = actual;
        }

    }
}

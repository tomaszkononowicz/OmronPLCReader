using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmronPLCTemperatureReader.Common
{
    public enum ConnectionStatusEnum
    {
        CONNECTING,
        CONNECTION_FAILED,
        CONNECTION_LOST,
        CONNECTED,
        RECONNECTING,
        DISCONNECTING,
        DISCONNECTED
    }
}

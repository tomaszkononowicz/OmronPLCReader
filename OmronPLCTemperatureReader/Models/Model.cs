using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmronPLCTemperatureReader.Models
{
    public class Model
    {
        Random random = new Random();
        //PLC

        public bool connect(string ip, string port)
        {
            return true;
        }

        public int getValue(uint DM)
        {
            return random.Next(300);
        }
    }
}

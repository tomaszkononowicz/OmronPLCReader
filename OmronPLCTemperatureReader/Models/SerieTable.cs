using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmronPLCTemperatureReader.Models
{
    class SerieTable
    {
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public double Value { get; set; }

        public SerieTable(string name, DateTime dateTime, double value)
        {
            this.Name = name;
            this.DateTime = dateTime;
            this.Value = value;
        }
    }
}

using OmronPLCTemperatureReader.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OmronPLCTemperatureReader.Models
{
    public class SerieOnline : Serie
    {

        [XmlIgnore]
        public double LastValue { get; private set; }

        public ushort Dm { get; set; }
        public new double Multiplier
        {
            get { return base.Multiplier; }
            set
            {
                LastValue = LastValue / base.Multiplier * value;
                base.Multiplier = value;
            }
        }


        public SerieOnline() : base() { }
        public SerieOnline(string name, ushort dm, double multiplier = 1) : base(name, multiplier)
        {
            Dm = dm;
        }
        public new void add(DateTime dateTime, int value)
        {
            LastValue = value * Multiplier;
            base.add(dateTime, value);
        }


    }
}

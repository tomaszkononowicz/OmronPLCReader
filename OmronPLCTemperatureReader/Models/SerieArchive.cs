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
    public class SerieArchive : Serie
    {
        public SerieArchive() : base() {  }
        public SerieArchive(string name, double multiplier = 1) : base(name, multiplier) { }
    }
}

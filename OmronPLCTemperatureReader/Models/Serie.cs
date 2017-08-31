using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmronPLCTemperatureReader.Models
{
    public class Serie
    {
        public string Name { get; set; }
        public int LastValue { get; set; }
        public string color;
        public ObservableCollection<KeyValuePair<DateTime, int>> Data { get; set; }
        public bool Visibility { get; set; }
        public uint Dm { get; set; }


        public Serie(string name)
        {
            Data = new ObservableCollection<KeyValuePair<DateTime, int>>();
            Name = name;
            Visibility = true;
        }

        public Serie(string name, int lastValue)
        {
            Data = new ObservableCollection<KeyValuePair<DateTime, int>>();
            Name = name;
            LastValue = lastValue;
            Visibility = true;
        }

        public void add(DateTime dateTime, int value)
        {
            Data.Add(new KeyValuePair<DateTime, int>(dateTime, value));
        }
    }
}

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
    [XmlInclude(typeof(SerieOnline))]
    public class Serie : INotifyPropertyChanged
    {
        public string Name { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlIgnore]
        public ObservableCollection<KeyValuePair<DateTime, double>> Data { get; set; }
        private bool visibility;
        public bool Visibility
        {
            get { return visibility; }
            set { visibility = value; OnPropertyChanged("Visibility"); }
        }
        private double multiplier;
        public double Multiplier
        {
            get { return multiplier; }
            set
            {
                lock (Data)
                {
                    foreach (KeyValuePair<DateTime, double> data in Data) {
                        data.Value = data.Value / multiplier * value;
                    }
                }
                multiplier = value;
            }
        }


        public Serie()
        {
            multiplier = 1;          
            Data = new ObservableCollection<KeyValuePair<DateTime, double>>();
            Visibility = true;

        }
        public Serie(string name, double multiplier = 1)
        {
            this.multiplier = multiplier;
            
            Data = new ObservableCollection<KeyValuePair<DateTime, double>>();
            Name = name;
            Visibility = true;
            Multiplier = multiplier;
        }

        public void add(DateTime dateTime, int value)
        {
            double _value = value * Multiplier;
            lock (Data)
            {               
                Data.Add(new KeyValuePair<DateTime, double>(dateTime, _value));     
            }
        }

        public void delete(KeyValuePair<DateTime, double> item)
        {
            lock (Data)
            {
                Data.Remove(item);
            }
        }

        public void delete(List<KeyValuePair<DateTime, double>> itemList)
        {
            foreach (KeyValuePair<DateTime, double> item in itemList) delete(item);
        }

        public List<KeyValuePair<DateTime, double>> findByDateTimeAndValue(DateTime dateTime, double value)
        {
            List<KeyValuePair<DateTime, double>> result = new List<KeyValuePair<DateTime, double>>();
            foreach (KeyValuePair<DateTime, double> item in Data)
            {
                if (item.Key.Equals(dateTime) && item.Value.Equals(value)) result.Add(item);
            }
            return result;
        }

        public bool ExistDateTime(DateTime dateTime)
        {
            foreach (KeyValuePair<DateTime, double> item in Data)
            {
                if (item.Key.Equals(dateTime))
                {
                    return true;
                }
            }
            return false;
        }

        public void saveToFile(DateTime dateTime, int value, string directory, string fileNamePrefix)
        {

            double _value = value * Multiplier;
            {
                FileStream fs = new FileStream(Path.Combine(directory, (fileNamePrefix == null || fileNamePrefix == "") ? fileNamePrefix + "_" : "" + dateTime.ToString("yyyy.MM.dd") + ".xml"), FileMode.Append);
                byte[] recordToSave = Encoding.ASCII.GetBytes(dateTime.ToString("yyyy.MM.dd H:mm:ss") + "\t" + Name + "\t" + _value + "\t" + Multiplier + "\r\n");
                fs.Write(recordToSave, 0, recordToSave.Length);
                fs.Close();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

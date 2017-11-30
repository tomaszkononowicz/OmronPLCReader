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
    public class Serie : INotifyPropertyChanged
    {
        public string Name { get; set; }
        [XmlIgnore]
        public double LastValue { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlIgnore]
        public ObservableCollection<KeyValuePair<DateTime, double>> Data { get; set; }
        private bool visibility;
        public bool Visibility
        {
            get { return visibility; }
            set { visibility = value; OnPropertyChanged("Visibility"); }
        }
        public ushort Dm { get; set; }
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
                LastValue = LastValue / multiplier * value;
                multiplier = value;
            }
        }


        public Serie()
        {
            multiplier = 1;          
            Data = new ObservableCollection<KeyValuePair<DateTime, double>>();
            Visibility = true;

        }
        public Serie(string name, ushort dm, double multiplier = 1)
        {
            this.multiplier = multiplier;
            
            Data = new ObservableCollection<KeyValuePair<DateTime, double>>();
            Name = name;
            Visibility = true;
            Dm = dm;
            Multiplier = multiplier;
        }

        public void add(DateTime dateTime, int value, string directory, string fileNamePrefix)
        {
            LastValue = value * Multiplier;
            lock (Data)
            {               
                Data.Add(new KeyValuePair<DateTime, double>(dateTime, LastValue));
                if (directory != null)
                {
                    try
                    {
                        FileStream fs = new FileStream(Path.Combine(directory, (fileNamePrefix == null || fileNamePrefix == "") ? fileNamePrefix + "_" : "" + dateTime.ToString("yyyy.MM.dd") + ".xml"), FileMode.Append);
                        byte[] recordToSave = Encoding.ASCII.GetBytes(dateTime.ToString("yyyy.MM.dd H:mm:ss") + "\t" + Name + "\t" + LastValue + "\t" + Multiplier + "\r\n");
                        fs.Write(recordToSave, 0, recordToSave.Length);
                        fs.Close();
                    }
                    catch { /*Console.WriteLine("Automatyczny zapis nieudany");*/ }
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

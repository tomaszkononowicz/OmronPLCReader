using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class SerieViewModel : ViewModelBase
    {

        #region UI properties

        public ObservableCollection<KeyValuePair<string, double>> Multipliers { get; set; }
        public KeyValuePair<string, double> MultiplierSelectedItem { get; set; }

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;               
                SetProperty(ref name, value);
            }
        }
        private ushort dm;
        public ushort Dm
        {
            get { return dm; }
            set
            {
                dm = value;
                SetProperty(ref dm, value);
            }
        }

        #endregion

        #region Commands properties and implementations

        public RelayCommand ColorPicker { get; set; }
        public RelayCommand Save { get; set; }
        public RelayCommand Cancel { get; set; }

        private bool CanCancel(object obj)
        {
            return true;
        }

        private void CancelAction(object obj)
        {
            Window window = obj as Window;
            window.DialogResult = false;
            window.Close();
        }

        private bool CanSave(object obj)
        {
            return true;
        }

        private void SaveAction(object obj)
        {
            Serie.Name = name;
            Serie.Dm = dm;
            Serie.Multiplier = MultiplierSelectedItem.Value;
            Window window = obj as Window;
            window.DialogResult = true;
            window.Close();
        }


        #endregion


        public string WindowTitle { get; set; }
        public SerieOnline Serie { get; set; }
        public SerieViewModel(ref SerieOnline serie)
        {
            WindowTitle = serie.Name;
            Serie = serie;
            Name = serie.Name;
            Dm = serie.Dm;
            Multipliers = new ObservableCollection<KeyValuePair<string, double>>()
            {
                new KeyValuePair<string, double>("1000", 1000),
                new KeyValuePair<string, double>("100", 100),
                new KeyValuePair<string, double>("10", 10),
                new KeyValuePair<string, double>("1 (Brak mnożnika)", 1),
                new KeyValuePair<string, double>("0,1", 0.1),
                new KeyValuePair<string, double>("0,01", 0.01),
                new KeyValuePair<string, double>("0,001", 0.001)
            };
            try
            {
                double multiplication = serie.Multiplier;
                MultiplierSelectedItem = Multipliers.Where(x => x.Value.Equals(multiplication)).First();
            } catch
            {
                MultiplierSelectedItem = new KeyValuePair<string, double>("1 (Brak mnożnika)", 1);
            }
            Save = new RelayCommand(SaveAction, CanSave);
            Cancel = new RelayCommand(CancelAction, CanCancel);
        }


    }
}

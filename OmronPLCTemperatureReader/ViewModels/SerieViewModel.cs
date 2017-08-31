using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class SerieViewModel : ViewModelBase
    {

        #region UI properties with errors and validation

        public Dictionary<string, string> PropertyErrors { get; set; }


        private string name;
        private string _name;
        public string Name
        {
            get { return (_name != null) ? _name : ""; }
            set
            {
                _name = value;               
                if (String.IsNullOrEmpty(value))
                {
                    PropertyErrors["Name"] = "To pole nie może zostać puste";
                }
                else
                {
                    name = value;
                    PropertyErrors.Remove("Name");
                }
                SetProperty(ref _name, value);
                OnPropertyChanged("PropertyErrors");
            }
        }
        private uint dm;
        private string _dm;
        public string Dm
        {
            get { return (_dm != null) ? _dm : ""; }
            set
            {
                _dm = value;
                try
                {
                    dm = uint.Parse(value);
                    PropertyErrors.Remove("Dm");
                }
                catch
                {
                    PropertyErrors["Dm"] = "Proszę podać liczbę większą od 0";
                }
                finally
                {
                    SetProperty(ref _dm, value);
                    OnPropertyChanged("PropertyErrors");
                }
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
            Window window = obj as Window;
            window.DialogResult = true;
            window.Close();
        }

        #endregion

   
        public string WindowTitle { get; set; }
        public Serie Serie { get; set; }
        public SerieViewModel(ref Serie serie)
        {
            PropertyErrors = new Dictionary<string, string>();
            WindowTitle = serie.Name;
            Serie = serie;
            Name = serie.Name;
            Dm = serie.Dm.ToString();
            Save = new RelayCommand(SaveAction, CanSave);
            Cancel = new RelayCommand(CancelAction, CanCancel);
        }


    }
}

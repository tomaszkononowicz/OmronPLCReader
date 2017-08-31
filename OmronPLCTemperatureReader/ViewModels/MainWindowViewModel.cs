using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Data;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region UI properties with errors and validation

        public Dictionary<string, string> PropertyErrors { get; set; }

        private IPAddress ip;
        private string _ip;
        public string Ip
        {
            get { return (_ip != null) ? _ip : ""; }
            set
            {
                if (Regex.IsMatch(value, @"^0*(25[0-5]|2[0-4]\d|1?\d\d?)(\.0*(25[05]|2[0-4]\d|1?\d\d?)){3}$"))
                {
                    ip = IPAddress.Parse(value);
                    PropertyErrors.Remove("Ip");
                }
                else
                {
                    PropertyErrors["Ip"] = "Niepoprawny format adresu IP: 4 liczby z zakresu 0-255 oddzielone kropkami np. 192.168.0.1";
                }
                SetProperty(ref _ip, value);
                OnPropertyChanged("PropertyErrors");

            }
        }

        private UInt16 port;
        private string _port;
        public string Port
        {
            get { return (_port != null) ? _port : ""; }
            set
            {
                try
                {
                    port = UInt16.Parse(value);
                    PropertyErrors.Remove("Port");
                }
                catch
                {
                    PropertyErrors["Port"] = "Niepoprawny format portu, proszę podać liczbę z zakresu od 1 do 65535";
                }
                finally
                {
                    SetProperty(ref _port, value);
                    OnPropertyChanged("PropertyErrors");
                }
            }
            
        }

        private int interval;
        private string _interval;
        public string Interval
        {
            get { return (_interval != null) ? _interval : ""; }
            set
            {
                try
                {
                    interval = int.Parse(value);
                    PropertyErrors["Interval"] = "";
                }
                catch
                {
                    PropertyErrors["Interval"] = "Niepoprawny format czasu, proszę podać liczbę sekund z zakresu od 1 do 3600";
                }
                finally
                {
                    SetProperty(ref _interval, value);
                    OnPropertyChanged("PropertyErrors");
                }
            }
        }

        private bool connected;
        public string ConnectionStatus
        {
            get
            {
                if (connected) return "Połączony";
                return "Rozłączony";
            }
        }

        public ObservableCollection<Serie> Series { get; set; }
        private Serie selectedItem;
        public Serie SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                OnPropertyChanged("ButtonHideShowSerieContent");
            }
        }

        #endregion

        #region Commands (properties and implementations)

        public RelayCommand ConnectDisconect { get; set; }
        public RelayCommand AddSerie { get; set; }
        public string ButtonHideShowSerieContent
        {
            get
            {
                if (selectedItem != null && !selectedItem.Visibility) return "Pokaz";
                return "Ukryj";
            }
        }
        public RelayCommand HideShowSerie { get; set; }
        public RelayCommand EditSerie { get; set; }
        public RelayCommand DeleteSerie { get; set; }
        public RelayCommand SortTableView { get; set; }
        public RelayCommand Copy { get; set; }


        private bool CanConnectDisconect(object obj)
        {
            if (PropertyErrors.ContainsKey("Ip") || PropertyErrors.ContainsKey("Port")) return false;
            return true;
        }
        private bool CanAddSerie(object obj)
        {
            return true;
        }

        private bool CanHideShowSerie(object obj)
        {
            return selectedItem != null ? true : false;
        }

        private bool CanEditSerie(object obj)
        {
            return selectedItem != null ? true : false;
        }

        private bool CanDeleteSerie(object obj)
        {
            return selectedItem != null ? true : false;
        }

        #endregion

        Model model;

        private bool? ascending;
        private string sortKey;
        public IEnumerable TableView
        {
            get
            {
                var list = (from s in Series
                            from d in s.Data
                            where s.Visibility == true
                            select new
                            {
                                Date = d.Key,
                                Serie = s.Name,
                                Value = d.Value
                            }).ToList();
                if (ascending == true)
                {
                    return list.OrderBy((x) =>
                    {
                        try
                        {
                            return x.GetType().GetProperty(sortKey).GetValue(x, null);
                        }
                        catch
                        {
                            return null;
                        }
                    });
                }
                if (ascending == false)
                {
                    return list.OrderByDescending((x) =>
                    {
                        try
                        {
                            return x.GetType().GetProperty(sortKey).GetValue(x, null);
                        }
                        catch
                        {
                            return null;
                        }
                    });
                }
                return list;
            }
        }

        private object selectedItemTableView;
        public object SelectedItemTableView
        {
            get { return selectedItemTableView; }
            set
            {
                selectedItemTableView = value;
            }
        }

        private Timer timer;


        Serie suszarka;



        public MainWindowViewModel()
        {
            PropertyErrors = new Dictionary<string, string>();
            Port = "0";
            Ip = "0.0.0.0";
            Interval = "1";
            connected = false;
            model = new Model();

            ConnectDisconect = new RelayCommand(ConnectDisconectAction, CanConnectDisconect);
            AddSerie = new RelayCommand(AddSerieAction, CanAddSerie);
            HideShowSerie = new RelayCommand(HideShowSerieAction, CanHideShowSerie);
            EditSerie = new RelayCommand(EditSerieAction, CanEditSerie);
            DeleteSerie = new RelayCommand(DeleteSerieAction, CanDeleteSerie);
            SortTableView = new RelayCommand(SortTableViewAction, CanSortTableView);
            Copy = new RelayCommand(CopyAction, CanCopy);
            Series = new ObservableCollection<Serie>();
            suszarka = new Serie("Suszarka", 150);
            timer = new Timer();
            timer.Interval = interval*100;
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;

            Series.Add(suszarka);
        }

        private bool CanCopy(object obj)
        {
            return true;
        }

        private void CopyAction(object obj)
        {
            MessageBox.Show("Skopiowano");
        }

        private bool CanSortTableView(object obj)
        {
            return true;
        }

        private void SortTableViewAction(object obj)
        {
            sortKey = sortKey ?? "";
            if (ascending == null || !sortKey.Equals(obj)) ascending = true;
            else if (ascending == true) ascending = false;
            else ascending = null;
            sortKey = obj as string;
            OnPropertyChanged("TableView");
        }



        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (Serie serie in Series)
            {
                int value = model.getValue(serie.Dm);
                DateTime now = DateTime.Now;
                serie.add(now, value);
                serie.LastValue = value;
                try
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => CollectionViewSource.GetDefaultView(Series).Refresh()));
                }
                catch { }

                if (SelectedItemTableView == null)
                {
                    OnPropertyChanged("TableView");
                }
            }
        }

        private void ConnectDisconectAction(object obj)
        {
            throw new NotImplementedException();
        }

        private void AddSerieAction(object obj)
        {
            Serie newSerie = new Serie("New Serie");
            Nullable<bool> dialogResult = new AddEditSerieWindow(ref newSerie).ShowDialog();
            if (dialogResult == true)
            {
                Series.Add(newSerie);                
            }
            OnPropertyChanged("TableView");
        }

        private void HideShowSerieAction(object obj)
        {
            
            if (selectedItem != null)
                selectedItem.Visibility = !selectedItem.Visibility;
            OnPropertyChanged("ButtonHideShowSerieContent");
            CollectionViewSource.GetDefaultView(Series).Refresh();
            OnPropertyChanged("TableView");
        }

        private void EditSerieAction(object obj)
        {
            if (selectedItem != null)
            {
                new AddEditSerieWindow(ref selectedItem).ShowDialog();
                CollectionViewSource.GetDefaultView(Series).Refresh();
            }
        }

        private void DeleteSerieAction(object obj)
        {
            if (MessageBox.Show("Usunąć serię " + selectedItem.Name + "?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == (MessageBoxResult.Yes))
            {
                Series.Remove(selectedItem);
                CollectionViewSource.GetDefaultView(Series).Refresh();
                OnPropertyChanged("TableView");
            }
        }
    }
}

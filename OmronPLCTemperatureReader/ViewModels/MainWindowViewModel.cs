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
        #region UI properties

        private IPAddress ip;
        public IPAddress Ip
        {
            get { return ip; }
            set
            {
                ip = value;
                SetProperty(ref ip, value);
            }
        }

        private UInt16 port;
        public UInt16 Port
        {
            get { return port; }
            set
            {
                port = value;
                SetProperty(ref port, value);
            }
        }

        private int interval;
        public int Interval
        {
            get { return interval; }
            set
            {
                interval = value;
                SetProperty(ref interval, value);
            }
        }

        public string ButtonHideShowSerieContent
        {
            get
            {
                if (selectedItem != null && !selectedItem.Visibility) return "Pokaz";
                return "Ukryj";
            }
        }
        public string ButtonConnectDisconnectContent
        {
            get { return model.Connected ? "Rozłącz" : "Połącz"; }
        }
        public bool CanEditConnectionSetting
        {
            get { return !model.Connected; }
        }

        public string ConnectionStatus
        {
            get
            {
                if (model.Connected) return "Połączony";
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
        public RelayCommand HideShowSerie { get; set; }
        public RelayCommand EditSerie { get; set; }
        public RelayCommand DeleteSerie { get; set; }
        public RelayCommand SortTableView { get; set; }
        public RelayCommand Copy { get; set; }


        private bool CanConnectDisconect(object obj)
        {
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

        Plc model = new Plc();

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
                                Value = d.Value * s.Multiplication
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

        private Timer getValuesTimer;
        private Timer connectionStatusTimer;


        Serie suszarka;



        public MainWindowViewModel()
        {
            Port = 9600;
            Ip = IPAddress.Parse("192.168.1.130");
            Interval = 1;

            ConnectDisconect = new RelayCommand(ConnectDisconectAction, CanConnectDisconect);
            AddSerie = new RelayCommand(AddSerieAction, CanAddSerie);
            HideShowSerie = new RelayCommand(HideShowSerieAction, CanHideShowSerie);
            EditSerie = new RelayCommand(EditSerieAction, CanEditSerie);
            DeleteSerie = new RelayCommand(DeleteSerieAction, CanDeleteSerie);
            SortTableView = new RelayCommand(SortTableViewAction, CanSortTableView);
            Copy = new RelayCommand(CopyAction, CanCopy);
            Series = new ObservableCollection<Serie>();
            suszarka = new Serie("Suszarka", 150);
            getValuesTimer = new Timer();
            getValuesTimer.Elapsed += GetValuesTimer_Elapsed;
            connectionStatusTimer = new Timer();
            connectionStatusTimer.Elapsed += ConnectionStatusTimer_Elapsed;
            connectionStatusTimer.Interval = 3000;
            connectionStatusTimer.Enabled = true;

            Series.Add(suszarka);
        }

        private void GetValuesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("ButtonConnectDisconnectContent");
            OnPropertyChanged("CanEditConnectionSetting");
            if (model.Connected)
            {
                for (int i = 0; i < Series.Count; i++)
                {
                    Serie serie = Series[i];
                    int? value = model.getValue(serie.Dm);
                    if (value != null)
                    {
                        int _value = (int)value;
                        DateTime now = DateTime.Now;
                        serie.add(now, _value);
                        serie.LastValue = _value;
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
            }
        }

        private void ConnectionStatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("ButtonConnectDisconnectContent");
            OnPropertyChanged("CanEditConnectionSetting");
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

        private void ConnectDisconectAction(object obj)
        {
            if (model.Connected)
            {
                if (model.disconnect())
                {
                    getValuesTimer.Enabled = false;
                }
            } else
            {
                if (model.connect(ip, port))
                {
                    getValuesTimer.Interval = interval * 1000;
                    getValuesTimer.Enabled = true;
                }
            }
            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("ButtonConnectDisconnectContent");
            OnPropertyChanged("CanEditConnectionSetting");
        }

        private void AddSerieAction(object obj)
        {
            Serie newSerie = new Serie("Nowa seria");
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

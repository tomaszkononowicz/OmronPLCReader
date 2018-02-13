using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Common;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Views;
using OxyPlot;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Xml;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region UI properties


        

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
                                Value = d.Value// * s.Multiplier
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

        #endregion

        #region Commands (properties and implementations)

        
        public RelayCommand AddSerie { get; set; }
        public RelayCommand HideShowSerie { get; set; }
        public RelayCommand EditSerie { get; set; }
        public RelayCommand DeleteSerie { get; set; }
        public RelayCommand SortTableView { get; set; }
        public RelayCommand Copy { get; set; }
        public RelayCommand Delete { get; set; }

        public RelayCommand ImportSeries { get; set; }
        public RelayCommand ExportSeries { get; set; }
        public RelayCommand DeleteAllSeries { get; set; }
        public RelayCommand OpenArchive { get; set; }



        #region Can

        private bool CanAlwaysTrue(object obj)
        {
            return true;
        }

        private bool CanAlwaysFalse(object obj)
        {
            return false;
        }

        private void CopyAction(object obj)
        {
            string copytext = "";
            IEnumerable items = obj as IEnumerable;
            foreach (var i in items) { 
                copytext += i.GetType().GetProperty("Date").GetValue(i, null).ToString() + "\t";
                copytext += i.GetType().GetProperty("Serie").GetValue(i, null).ToString() + "\t";
                copytext += i.GetType().GetProperty("Value").GetValue(i, null).ToString() + "\r\n";
                Clipboard.SetText(copytext);
            }        
        }

        private void DeleteAction(object obj)
        {
            IEnumerable items = obj as IEnumerable;
            foreach (var i in items)
            {
                DateTime dateTime = DateTime.Parse(i.GetType().GetProperty("Date").GetValue(i, null).ToString());
                int value = int.Parse(i.GetType().GetProperty("Value").GetValue(i, null).ToString());
                foreach (Serie s in Series)
                {
                    s.delete(s.findByDateTimeAndValue(dateTime, value));
                }
            }
            OnPropertyChanged("TableView");
            PlotViewModel.InvalidatePlot(true);
        }

        


        #endregion
        #region Action

        private void SortTableViewAction(object obj)
        {
            sortKey = sortKey ?? "";
            if (ascending == null || !sortKey.Equals(obj)) ascending = true;
            else if (ascending == true) ascending = false;
            else ascending = null;
            sortKey = obj as string;
            OnPropertyChanged("TableView");
        }      

        private void AddSerieAction(object obj)
        {
            SerieOnline newSerie = new SerieOnline("Nowa seria", 0);
            Nullable<bool> dialogResult = new AddEditSerieWindow(ref newSerie).ShowDialog();
            if (dialogResult == true)
            {
                Series.Add(newSerie);
                newSerie.Data.CollectionChanged += Data_CollectionChanged;
            }
            OnPropertyChanged("TableView");
            PlotViewModel.InvalidatePlot(true);
        }

        private void HideShowSerieAction(object obj)
        {
            Serie selectedItem = obj as Serie;
            if (selectedItem != null)
            {               
                selectedItem.Visibility = !selectedItem.Visibility;
            }
            OnPropertyChanged("ButtonHideShowSerieContent");
            CollectionViewSource.GetDefaultView(Series).Refresh();
            OnPropertyChanged("TableView");
            PlotViewModel.InvalidatePlot(true);
        }

        private void EditSerieAction(object obj)
        {
            SerieOnline selectedItem = obj as SerieOnline;
            if (selectedItem != null)
            {
                new AddEditSerieWindow(ref selectedItem).ShowDialog();
                CollectionViewSource.GetDefaultView(Series).Refresh();
                OnPropertyChanged("TableView");
                PlotViewModel.InvalidatePlot(true);

            }
        }

        private void DeleteSerieAction(object obj)
        {
            Serie selectedItem = obj as Serie;
            if (selectedItem != null)
            {
                if (MessageBox.Show("Usunąć serię " + selectedItem.Name + "?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == (MessageBoxResult.Yes))
                {
                    Series.Remove(selectedItem);
                    CollectionViewSource.GetDefaultView(Series).Refresh();
                    OnPropertyChanged("TableView");
                    PlotViewModel.InvalidatePlot(true);
                }
            }
        }
        #endregion

        #endregion

        #region Timers elapsed

        private void GetValuesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            if (plc.ConnectionStatus == ConnectionStatusEnum.CONNECTED)
            {
                OnPropertyChanged("ConnectionStatus");
                OnPropertyChanged("ButtonConnectDisconnectContent");
                OnPropertyChanged("CanEditConnectionSetting");

                for (int i = 0; i < Series.Count; i++)
                {

                    SerieOnline serie = Series[i] as SerieOnline;
                    if (serie != null)
                    {
                        int? value = plc.getValue(serie.Dm);
                        DateTime now = DateTime.Now;
                        if (value != null)
                        {
                            int _value = (int)value;
                            now = new DateTime(
                            now.Year,
                            now.Month,
                            now.Day,
                            now.Hour,
                            now.Minute,
                            now.Second);
                            serie.add(now, _value);
                        }
                    }
                }
                try
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => CollectionViewSource.GetDefaultView(Series).Refresh()));
                    //ChartMove();
                    PlotViewModel.InvalidatePlot(true);
                }
                catch { }

                if (SelectedItemTableView == null)
                {
                    OnPropertyChanged("TableView");
                }
            }

        }

        #endregion



        


        public ObservableCollection<Serie> Series { get; set; }
        /// <summary>
        /// ConnectionRefusedTimes[even] = startConnectionRefused
        /// ConnectionRefusedTimes[odd] = stopConnecectionRefused
        /// </summary>
        public ObservableCollection<DateTime> ConnectionRefusedTimes { get; set; }
        private PlotModel plot = new PlotModel();
        public ConnectionViewModel ConnectionViewModel { get; private set; }
        public PlotViewModel PlotViewModel { get; private set; }
        private Plc plc = new Plc();
        private System.Timers.Timer getValuesTimer;



       // Serie suszarka;

        private bool LoadSettings(string path)
        {
            
            string loadSettingsLastError;
            try
            {
                XmlDocument settings = new XmlDocument();
                loadSettingsLastError = "Błąd podczas ładowania pliku " + path;
                settings.Load(path);
                loadSettingsLastError = "Brak korzenia w pliku ustawień";
                XmlNode settingsRoot = settings.DocumentElement;
                try {
                    string defaultSeriesFilePath = settingsRoot.SelectSingleNode("DefaultSeriesFilePath").InnerText;
                    Series = new ObservableCollection<Serie>();
                    ImportSeriesXML(defaultSeriesFilePath);
                }
                catch { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public MainWindowViewModel()
        {
            //Load settings from settings.xml
            //string settingsFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "settings.xml");
            string settingsFileName = "settings.xml";
            LoadSettings(settingsFileName);
            //As default in properties region

            if (Series == default(ObservableCollection<Serie>)) Series = new ObservableCollection<Serie>();

            ConnectionViewModel = new ConnectionViewModel(plc);
            PlotViewModel = new PlotViewModel(plot, Series);


            plc.ConnectionStatusChanged += Plc_ConnectionStatusChanged;
            plc.ConnectionStatusChanged += PlotViewModel.ConnectionStatusChanged;


            
            AddSerie = new RelayCommand(AddSerieAction, CanAlwaysTrue);
            HideShowSerie = new RelayCommand(HideShowSerieAction, CanAlwaysTrue);
            EditSerie = new RelayCommand(EditSerieAction, CanAlwaysTrue);
            DeleteSerie = new RelayCommand(DeleteSerieAction, CanAlwaysTrue);
            SortTableView = new RelayCommand(SortTableViewAction, CanAlwaysTrue);
            Copy = new RelayCommand(CopyAction, CanAlwaysTrue);
            Delete = new RelayCommand(DeleteAction, CanAlwaysTrue);
            
            ImportSeries = new RelayCommand(ImportSeriesAction, CanAlwaysTrue);
            ExportSeries = new RelayCommand(ExportSeriesAction, CanAlwaysTrue);
            DeleteAllSeries = new RelayCommand(DeleteAllSeriesAction, CanAlwaysTrue);
            OpenArchive = new RelayCommand(OpenArchiveAction, CanAlwaysTrue);


            Series.CollectionChanged += Series_CollectionChanged;
            //suszarka = new Serie("Suszarka", 150);
            getValuesTimer = new System.Timers.Timer();
            getValuesTimer.Elapsed += GetValuesTimer_Elapsed;


            //Series.Add(suszarka);

            


            //TODO 
            //Strzałka aby rozciągnąć Y na górę i dół na maksa


            //getValuesTimer.Interval = interval * 1000;
            //getValuesTimer.Enabled = true;




        }


        //SeriesVievModel

        private void OpenArchiveAction(object obj)
        {
            MessageBox.Show("Przeglądarka archiwum jeszcze nie iestnieje, pojawi się w kolejnej wersji programu. Jest to jedyna funkcjonalnośc jeszcze nie zaimplementowana.", "Nie zaimplementowano jeszcze", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
        }

        private void ExportSeriesAction(object obj)
        {
            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "Xml file (*.xml)|*.xml";
            var result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string path = Path.GetFullPath(saveFileDialog.FileName);
                if (!ExportSeriesXML(path)) MessageBox.Show("Nie udało się wyeksportować serii", "Eksport", MessageBoxButton.OK, MessageBoxImage.Error);
            }        
        }

        private bool ExportSeriesXML(string path)
        {
            try
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(Series.GetType());
                FileStream fs = new FileStream(path, FileMode.Create);
                serializer.Serialize(fs, Series);
                fs.Close();
                return true;
            }
            catch { return false; }
        }

        private void ImportSeriesAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Xml file (*.xml)|*.xml";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string path = Path.GetFullPath(openFileDialog.FileName);
                if (!ImportSeriesXML(path)) MessageBox.Show("Nie udało się zaimportować serii", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ImportSeriesXML(string path)
        {
            try
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof (ObservableCollection<SerieOnline>));
                FileStream fs = new FileStream(path, FileMode.Open);
                ObservableCollection<SerieOnline> seriesRead = (ObservableCollection<SerieOnline>)serializer.Deserialize(fs);
                fs.Close();
                foreach (SerieOnline s in seriesRead) Series.Add(s);
                return true;
            }
            catch { return false; }
        }

        private void DeleteAllSeriesAction(object obj)
        {
            if (MessageBox.Show("Usunąć wszystkie serie?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == (MessageBoxResult.Yes))
            {
                while (Series.Count > 0)
                {
                    Series.Remove(Series[0]);
                    CollectionViewSource.GetDefaultView(Series).Refresh();
                    OnPropertyChanged("TableView");
                    PlotViewModel.InvalidatePlot(true);
                }
            }
        }

      
            
        

        private void Plc_ConnectionStatusChanged(object sender, ConnectionStatusChangedArgs e)
        {
            
            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("ButtonConnectDisconnectContent");
            OnPropertyChanged("CanEditConnectionSetting");
            switch (e.Actual)
            {
                case ConnectionStatusEnum.CONNECTED:
                    //TODO
                    //Czyszczenie serii?
                    //If series != null, plot.Axes[0].Minimum = plot.Axes[0].DataMinimum?
                    getValuesTimer.Interval = ConnectionViewModel.Interval * 1000;
                    getValuesTimer.Enabled = true;
                    break;
                default:
                    getValuesTimer.Enabled = false;
                    break;
            }

        }


        //ChartViewModel

       
        private void Data_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            //{
            //    DataCounter++; ;
            //    int serieCount = Series.Count;
            //    if (DataCounter > 1000)
            //    {
            //        for (int i = 0; i < serieCount; i++)
            //        {
            //            Serie s = Series[i];
            //            s.Data.Clear();
            //           // s.add(DateTime.Now, s.LastValue);
            //        }
            //    }
            //    OnPropertyChanged("DataCounter");
            //}

        }

        private void Series_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            //{
            //    DataCounter = 0;
            //    int serieCount = Series.Count;
            //    for (int i = 0; i < serieCount; i++)
            //    {
            //        Serie s = Series[i];
            //        DataCounter += s.Data.Count;
            //    }
            //    OnPropertyChanged("DataCounter");

            //}
        }

    }
}

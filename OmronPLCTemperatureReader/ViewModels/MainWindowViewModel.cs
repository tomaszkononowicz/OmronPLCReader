using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Common;
using OmronPLCTemperatureReader.Common.Converters;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Views;
using OxyPlot;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;
using System.Configuration;
using System.Reflection;
using System.Diagnostics;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        


        #region Timers elapsed
        private string LogsFolderPath;
        private string LogsFilePrefix;
        private StringBuilder StringBuilder;
        //Dodac powyższe w ścieżce niżej

        private void GetValuesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            if (plc.ConnectionStatus == ConnectionStatusEnum.CONNECTED)
            {
                for (int i = 0; i < SeriesOnline.Count; i++)
                {

                    SerieOnline serie = SeriesOnline[i] as SerieOnline;
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

                            try
                            {
                                StringBuilder.Clear();
                                using (FileStream fs = new FileStream(Path.Combine(LogsFolderPath ?? string.Empty, StringBuilder.Append(LogsFilePrefix).Append(DateTime.Now.ToString("yyyy.MM.dd")).Append(".txt").ToString()), FileMode.Append))
                                {
                                    StringBuilder.Clear();
                                    var lineToWrite = StringBuilder.Append(serie.Name).Append("\t").Append(now).Append("\t").Append(_value * serie.Multiplier).Append("\r\n").ToString();
                                    byte[] bytesToWrite = new UTF8Encoding(true).GetBytes(lineToWrite);
                                    fs.Write(bytesToWrite, 0, bytesToWrite.Length);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"{ex.Message}");
                            }
                        }
                    }
                }
                try
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => CollectionViewSource.GetDefaultView(SeriesOnline).Refresh()));
                    PlotViewModel.ChartMove();
                    PlotViewModel.InvalidatePlot(true);
                }
                catch { }

                //if (SelectedItemTableView == null)
                //{
                TableViewModel.Refresh();
                //}
            }

        }

        #endregion
        

        public RelayCommand EditSettings { get; set; }



        public ObservableCollection<Serie> SeriesArchive { get; set; }

        public ObservableCollection<Serie> SeriesOnline { get; set; }

        public string Version
        {
            get
            {
                var productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
                return string.Format("v {0}", productVersion);
            }
        }

        /// <summary>
        /// ConnectionRefusedTimes[even] = startConnectionRefused
        /// ConnectionRefusedTimes[odd] = stopConnecectionRefused
        /// </summary>
        public ObservableCollection<DateTime> ConnectionRefusedTimes { get; set; }
        private PlotModel plot = new PlotModel();
        public ConnectionViewModel ConnectionViewModel { get; private set; }
        public PlotViewModel PlotViewModel { get; private set; }
        public TableViewModel TableViewModel { get; private set; }
        public SeriesOnlineDataGridModelView SeriesOnlineDataGridViewModel { get; private set; }
        
        private PlotModel plotArchive = new PlotModel();
        public PlotViewModel PlotArchiveViewModel { get; private set; }
        public TableViewModel TableArchiveViewModel { get; private set; }
        public SeriesArchiveDataGridViewModel SeriesArchiveDataGridViewModel { get; private set; }

        private int selectedTabIndex;
        public int SelectedTabIndex {
            get
            {
                return selectedTabIndex;
            }
            set
            {
                selectedTabIndex = value;
                if (selectedTabIndex > 1)
                {
                    PlotViewModel.Visibility = Visibility.Collapsed;
                    PlotArchiveViewModel.Visibility = Visibility.Visible;
                    SeriesOnlineDataGridViewModel.Visibility = Visibility.Collapsed;
                    SeriesArchiveDataGridViewModel.Visibility = Visibility.Visible;
                }
                else
                {
                    PlotViewModel.Visibility = Visibility.Visible;
                    PlotArchiveViewModel.Visibility = Visibility.Collapsed;
                    SeriesOnlineDataGridViewModel.Visibility = Visibility.Visible;
                    SeriesArchiveDataGridViewModel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private Plc plc = new Plc();
        private System.Timers.Timer getValuesTimer;

        private bool LoadSettings()
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.IP))
                {
                    ConnectionViewModel.Ip = IPAddress.Parse(Properties.Settings.Default.IP);
                }
                ConnectionViewModel.Port = Properties.Settings.Default.Port;
                ConnectionViewModel.Interval = Properties.Settings.Default.Interval;
                LogsFolderPath = Properties.Settings.Default.LogsFolderPath;
                LogsFilePrefix = Properties.Settings.Default.LogsFilePrefix;
                SeriesOnlineDataGridViewModel.ImportSeriesFromFile(Properties.Settings.Default.SeriesFilePath);
                SeriesOnlineDataGridViewModel.ImportConnectionFromFile(Properties.Settings.Default.SeriesFilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }


        public MainWindowViewModel()
        {
            this.ParentViewModel = null;
            this.StringBuilder = new StringBuilder();
            SeriesOnline = new ObservableCollection<Serie>();
            SeriesArchive = new ObservableCollection<Serie>();

            ConnectionViewModel = new ConnectionViewModel(this, plc);
            ConnectionViewModel.CommandHandler += ViewModel_CommandHandler;

            //Load settings from settings.xml
            //string settingsFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "settings.xml")
            
            //As default in properties region


            PlotViewModel = new PlotViewModel(this, plot, SeriesOnline);
            PlotViewModel.CommandHandler += ViewModel_CommandHandler;
            TableViewModel = new TableViewModel(this, SeriesOnline);
            TableViewModel.CommandHandler += ViewModel_CommandHandler;
            SeriesOnlineDataGridViewModel = new SeriesOnlineDataGridModelView(this, SeriesOnline);
            SeriesOnlineDataGridViewModel.CommandHandler += ViewModel_CommandHandler;
            
            PlotArchiveViewModel = new PlotViewModel(this, plotArchive, SeriesArchive);
            PlotArchiveViewModel.CommandHandler += ViewModel_CommandHandler;
            TableArchiveViewModel = new TableViewModel(this, SeriesArchive);
            TableArchiveViewModel.CommandHandler += ViewModel_CommandHandler;
            SeriesArchiveDataGridViewModel = new SeriesArchiveDataGridViewModel(this, SeriesArchive);
            SeriesArchiveDataGridViewModel.CommandHandler += ViewModel_CommandHandler;

            EditSettings = new RelayCommand(EditSettingsAction);
            LoadSettings();

            plc.ConnectionStatusChanged += Plc_ConnectionStatusChanged;
            plc.ConnectionStatusChanged += PlotViewModel.ConnectionStatusChanged;
            //ConnectionViewModel.Refresh();




            SeriesOnline.CollectionChanged += Series_CollectionChanged;
            //suszarka = new Serie("Suszarka", 150);
            getValuesTimer = new System.Timers.Timer();
            getValuesTimer.Elapsed += GetValuesTimer_Elapsed;
            SelectedTabIndex = 0;

            //Series.Add(suszarka);




            //TODO 
            //Strzałka aby rozciągnąć Y na górę i dół na maksa


            //getValuesTimer.Interval = interval * 1000;
            //getValuesTimer.Enabled = true;




        }





        //SerieOnline selectedItem = obj as SerieOnline;
        //    if (selectedItem != null)
        //    {
        //        new AddEditSerieWindow(ref selectedItem).ShowDialog();
        //CollectionViewSource.GetDefaultView(Series).Refresh();
        //        Command("Table.Refresh");
        //        Command("Plot.Refresh");









    private void EditSettingsAction(object obj)
        {
            var result = new SettingsWindow(new SettingsViewModel(this)).ShowDialog();
            if (result.HasValue && result.Value)
            {
                LogsFolderPath = Properties.Settings.Default.LogsFolderPath;
                LogsFilePrefix = Properties.Settings.Default.LogsFilePrefix;
            }
        }

        private void ViewModel_CommandHandler(object sender, string e)
        {
            switch (e)
            {
                case "Plot.Refresh":
                    PlotViewModel.InvalidatePlot(true);
                    PlotArchiveViewModel.InvalidatePlot(true);
                    break;
                case "Table.Refresh":
                    TableViewModel.Refresh();
                    TableArchiveViewModel.Refresh();
                    break;
                case "DataGrid.Refresh":
                    SeriesOnlineDataGridViewModel.Refresh();
                    SeriesArchiveDataGridViewModel.Refresh();
                    break;             
            }
        }



        //SeriesVievModel

        

      
            
        

        private void Plc_ConnectionStatusChanged(object sender, ConnectionStatusChangedArgs e)
        {
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

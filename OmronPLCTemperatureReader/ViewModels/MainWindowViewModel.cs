using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Common;
using OmronPLCTemperatureReader.Common.Converters;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Views;
using OxyPlot;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Xml;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        


        #region Timers elapsed

        private void GetValuesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            if (plc.ConnectionStatus == ConnectionStatusEnum.CONNECTED)
            {
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



        


        public ObservableCollection<Serie> Series { get; set; }
        /// <summary>
        /// ConnectionRefusedTimes[even] = startConnectionRefused
        /// ConnectionRefusedTimes[odd] = stopConnecectionRefused
        /// </summary>
        public ObservableCollection<DateTime> ConnectionRefusedTimes { get; set; }
        private PlotModel plot = new PlotModel();
        public ConnectionViewModel ConnectionViewModel { get; private set; }
        public TableViewModel TableViewModel { get; private set; }
        
        public PlotViewModel PlotViewModel { get; private set; }
        public SeriesDataGridViewModel SeriesOnlineDataGridViewModel { get; private set; }
        public SeriesDataGridViewModel SeriesArchiveDataGridViewModel { get; private set; }
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

        private bool ImportSeriesXML(string path)
        {
            try
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<SerieOnline>));
                FileStream fs = new FileStream(path, FileMode.Open);

                ObservableCollection<SerieOnline> seriesRead = (ObservableCollection<SerieOnline>)serializer.Deserialize(fs);
                fs.Position = fs.Position - 30;
                try
                {
                    if (ConnectionViewModel.CanEditConnectionSetting)
                    {
                        byte[] result = new byte[30];
                        fs.Read(result, 0, 30);
                        string str = Encoding.ASCII.GetString(result);
                        string ip;
                        string port;
                        string[] splitted = str.Split('|');
                        ip = splitted[1];
                        port = splitted[2];

                        ConnectionViewModel.Ip = IPAddress.Parse(ip);
                        ConnectionViewModel.Port = ushort.Parse(port);
                        ConnectionViewModel.Refresh();
                    }
                }
                catch { };
                fs.Close();
                foreach (SerieOnline s in seriesRead) Series.Add(s);
                return true;
            }
            catch {
                MessageBox.Show("Nie udało się zaimportować serii", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ExportSeriesXML(string path)
        {
            try
            {
                //Export only Series Online
                ObservableCollection<SerieOnline> toExport = new ObservableCollection<SerieOnline>();
                foreach (Serie serie in Series)
                {
                    SerieOnline serieOnline = serie as SerieOnline;
                    if (serieOnline != null)
                    {
                        toExport.Add(serieOnline);
                    }
                }
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(toExport.GetType());
                FileStream fs = new FileStream(path, FileMode.Create);
                serializer.Serialize(fs, toExport);
                //IP in last line as comment
                byte[] connectionInfo = Encoding.ASCII.GetBytes("<!--|" + ConnectionViewModel.Ip + "|" + ConnectionViewModel.Port + "|-->");
                fs.Write(connectionInfo, 0, connectionInfo.Length);
                fs.Close();
                return true;
            }
            catch {
                MessageBox.Show("Nie udało się wyeksportować serii", "Eksport", MessageBoxButton.OK, MessageBoxImage.Error);
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
            ConnectionViewModel.CommandHandler += ViewModel_CommandHandler;
            PlotViewModel = new PlotViewModel(plot, Series);
            PlotViewModel.CommandHandler += ViewModel_CommandHandler;
            TableViewModel = new TableViewModel(Series);
            TableViewModel.CommandHandler += ViewModel_CommandHandler;
            SeriesOnlineDataGridViewModel = new SeriesOnlineDataGridModelView(Series);
            SeriesOnlineDataGridViewModel.CommandHandler += ViewModel_CommandHandler;
            SeriesArchiveDataGridViewModel = new SeriesArchiveDataGridModelView(Series);
            SeriesArchiveOnlineDataGridViewModel.CommandHandler += ViewModel_CommandHandler;

            plc.ConnectionStatusChanged += Plc_ConnectionStatusChanged;
            plc.ConnectionStatusChanged += PlotViewModel.ConnectionStatusChanged;
            //ConnectionViewModel.Refresh();

            
           


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

        private void ViewModel_CommandHandler(object sender, string e)
        {
            switch (e)
            {
                case "Plot.Refresh":
                    PlotViewModel.InvalidatePlot(true);
                    break;
                case "Table.Refresh":
                    TableViewModel.Refresh();
                    break;
                case "DataGrid.Refresh":
                    SeriesOnlineDataGridModelView.Refresh();
                    break;
                case "Main.ImportXML":
                    ImportSeriesXML(SeriesOnlineDataGridModelView.FilePath);
                    break;
                case "Main.ExportXML":
                    ExportSeriesXML(SeriesOnlineDataGridModelView.FilePath);
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

using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Common;
using OmronPLCTemperatureReader.Common.ValidationRules;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Views;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Xml;
using System.Xml.XPath;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region UI properties

        #region Chart properties
        private object chartLock = new object();

        //private bool chartXDuration;
        //public bool ChartXDuration
        //{
        //    get { return chartXDuration; }
        //    set
        //    {
        //        chartXDuration = value;
        //        SetProperty(ref chartXDuration, value);
        //    }
        //}
        public int ChartXDurationValue { get; set; }
        public bool ChartFlow { get; set; }
        public bool ChartFlowOnEdge { get; set; }
        public bool ChartFlowMinLock { get; set; }
        public DateTime ChartDateXMin { get; set; }
        public DateTime ChartDateXMax { get; set; }
        public int ChartYMin { get; set; }
        public int ChartYMax { get; set; }






        private string chartTitle;
        public string ChartTitle
        {
            get { return chartTitle; }
            set
            {
                //Property over Plot.Title because when edit Title via Textbox will get exception => this property must return chartTitle, not Plot.Title
                chartTitle = value;
                plot.Title = chartTitle;
                Plot.InvalidatePlot(true);
            }
        }

        public bool ChartLegendIsVisible
        {
            get { return Plot.IsLegendVisible; }
            set
            {
                Plot.IsLegendVisible = value;
                Plot.InvalidatePlot(true);
            }
        }



        #endregion

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

        public string ButtonConnectDisconnectContent
        {
            get
            {
                switch (plc.ConnectionStatus)
                {
                    case ConnectionStatusEnum.CONNECTED: return "Rozłącz";
                    case ConnectionStatusEnum.CONNECTING:
                    case ConnectionStatusEnum.RECONNECTING: return "Przerwij";
                    default: return "Połącz";
                }
            }
        }
        public bool CanEditConnectionSetting
        {
            get { switch (plc.ConnectionStatus) {
                    case ConnectionStatusEnum.CONNECTED:
                    case ConnectionStatusEnum.CONNECTING:
                    case ConnectionStatusEnum.DISCONNECTING:
                    case ConnectionStatusEnum.RECONNECTING: return false;
                    default: return true;
                }
            }
        }

        public string ConnectionStatus
        {
            get
            {
                switch (plc.ConnectionStatus)
                {
                    case ConnectionStatusEnum.CONNECTED: return "Połączony";
                    case ConnectionStatusEnum.CONNECTING: return "Łączenie...";
                    case ConnectionStatusEnum.CONNECTION_FAILED: return "Połączenie nieudane";
                    case ConnectionStatusEnum.CONNECTION_LOST: return "Połączenie przerwane";
                    case ConnectionStatusEnum.RECONNECTING: return "Ponowne łączenie... ";// + plc.AutoReconnectAfterConnectionLostCounter + "/" + plc.AutoReconnectAfterConnectionLostMax; 
                    case ConnectionStatusEnum.DISCONNECTED: return "Rozłączony";
                    case ConnectionStatusEnum.DISCONNECTING: return "Rozłączanie...";
                    default: return "Rozłączony";
                }
            }
        }


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

        public RelayCommand ConnectDisconect { get; set; }
        CancellationTokenSource connectCancellationTokenSource = new CancellationTokenSource();
        public RelayCommand AddSerie { get; set; }
        public RelayCommand HideShowSerie { get; set; }
        public RelayCommand EditSerie { get; set; }
        public RelayCommand DeleteSerie { get; set; }
        public RelayCommand SortTableView { get; set; }
        public RelayCommand Copy { get; set; }
        public RelayCommand ChartXDurationSet { get; set; }
        public RelayCommand ChartXRangeSet { get; set; }
        public RelayCommand ChartYRangeSet { get; set; }
        public RelayCommand ChartShow { get; set; }
        public RelayCommand ChartMoveToEnd { get; set; }
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
            IEnumerable series = obj as IEnumerable;
            foreach (var s in series) {
                copytext += s.GetType().GetProperty("Date").GetValue(s, null).ToString() + "\t";
                copytext += s.GetType().GetProperty("Serie").GetValue(s, null).ToString() + "\t";
                copytext += s.GetType().GetProperty("Value").GetValue(s, null).ToString() + "\r\n";
                Clipboard.SetText(copytext);
            }


            
            
        }

        private bool CanChartYRangeSet(object obj)
        {
            if (ChartYMin >= ChartYMax) return false;
            return true;
        }

        private bool CanChartXRangeSet(object obj)
        {
            if (ChartDateXMin >= ChartDateXMax) return false;
            return true;
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

        private void ConnectDisconectAction(object obj)
        {
            if (plc.ConnectionStatus == ConnectionStatusEnum.CONNECTING ||
                plc.ConnectionStatus == ConnectionStatusEnum.RECONNECTING)
            {
                connectCancellationTokenSource.Cancel();
                plc.disconnect();
                connectCancellationTokenSource = new CancellationTokenSource();
            }
            else if (plc.ConnectionStatus == ConnectionStatusEnum.CONNECTED)
            {
                if (plc.disconnect())
                {
                    getValuesTimer.Enabled = false;
                }
            }
            else if (plc.ConnectionStatus != ConnectionStatusEnum.CONNECTING && 
                     plc.ConnectionStatus != ConnectionStatusEnum.DISCONNECTING &&
                     plc.ConnectionStatus != ConnectionStatusEnum.RECONNECTING)
            {
                Task.Run(new Action(() =>
                {
                    if (plc.connect(ip, port))
                    {
                        //TODO
                        //Czyszczenie serii?
                        //If series != null, plot.Axes[0].Minimum = plot.Axes[0].DataMinimum?
                        plot.ResetAllAxes();
                        plot.Axes[0].Minimum = DateTimeAxis.ToDouble(DateTime.Now);
                        plot.Axes[0].Maximum = DateTimeAxis.ToDouble(DateTime.Now.AddSeconds(interval*10));
                        plot.Axes[0].Reset();
                        Plot.InvalidatePlot(false);
                        getValuesTimer.Interval = interval * 1000;
                        getValuesTimer.Enabled = true;
                    }
                }), connectCancellationTokenSource.Token);
            }
        }

        private void AddSerieAction(object obj)
        {
            Serie newSerie = new Serie("Nowa seria", 0);
            Nullable<bool> dialogResult = new AddEditSerieWindow(ref newSerie).ShowDialog();
            if (dialogResult == true)
            {
                Series.Add(newSerie);
                newSerie.Data.CollectionChanged += Data_CollectionChanged;
            }
            OnPropertyChanged("TableView");
            Plot.InvalidatePlot(true);
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
            Plot.InvalidatePlot(true);
        }

        private void EditSerieAction(object obj)
        {
            Serie selectedItem = obj as Serie;
            if (selectedItem != null)
            {
                new AddEditSerieWindow(ref selectedItem).ShowDialog();
                CollectionViewSource.GetDefaultView(Series).Refresh();
                OnPropertyChanged("TableView");
                Plot.InvalidatePlot(true);

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
                    Plot.InvalidatePlot(true);
                }
            }
        }
        #endregion

        #endregion

        #region Timers elapsed

        private void GetValuesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("ButtonConnectDisconnectContent");
            OnPropertyChanged("CanEditConnectionSetting");
            for (int i = 0; i < Series.Count; i++)
            {

                Serie serie = Series[i];
                int? value = plc.getValue(serie.Dm);
                if (value != null)
                {
                    int _value = (int)value;
                    DateTime now = DateTime.Now;
                    now = new DateTime(
                    now.Year,
                    now.Month,
                    now.Day,
                    now.Hour,
                    now.Minute,
                    now.Second);
                }
            }
            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => CollectionViewSource.GetDefaultView(Series).Refresh()));
                ChartMove();
                Plot.InvalidatePlot(true);
            }
            catch { }

            if (SelectedItemTableView == null)
            {
                OnPropertyChanged("TableView");
            }

        }

        #endregion


        private PlotModel plot;
        
        private double? lastXAxisDataMaximum;
        public PlotModel Plot
        {
            get
            {


                plot.Series.Clear();
                //Dla każdej serii utwórzyć Lineseries i do tego lineseries 
                for (int i = 0; i < Series.Count; i++)
                {
                    Serie s = Series[i];
                    LineSeries lineSeries = new LineSeries();
                    lineSeries.Title = s.Name;
                    lineSeries.IsVisible = s.Visibility;
                    foreach (KeyValuePair<DateTime, double> pair in s.Data) //TODO Modyfikacja kolekcji!!
                    {
                        lineSeries.Points.Add(DateTimeAxis.CreateDataPoint(pair.Key, pair.Value));                     
                    }
                    plot.Series.Add(lineSeries);
                }

                return plot;
            }

            set
            {
                plot = value;
                SetProperty(ref plot, value);
            }
        }

        public ObservableCollection<Serie> Series { get; set; }
        public int DataCounter { get; set; }

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
                try { Ip = IPAddress.Parse(settingsRoot.SelectSingleNode("Ip").InnerText); }
                catch { }
                try { Port = ushort.Parse(settingsRoot.SelectSingleNode("Port").InnerText); }
                catch { }
                try { Interval = int.Parse(settingsRoot.SelectSingleNode("Interval").InnerText); }
                catch { }
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
            if (Port == default(ushort)) Port = 9600;
            //Ip = IPAddress.Parse("192.168.1.130");
            if (Ip == default(IPAddress)) Ip = IPAddress.Parse("194.187.238.5");
            if (Interval == default(int)) Interval = 1;
            if (Series == default(ObservableCollection<Serie>)) Series = new ObservableCollection<Serie>();


            ChartDateXMin = DateTime.Now;
            ChartDateXMax = DateTime.Now;

            plc.ConnectionStatusChanged += Plc_ConnectionStatusChanged;


            ConnectDisconect = new RelayCommand(ConnectDisconectAction, CanAlwaysTrue);
            AddSerie = new RelayCommand(AddSerieAction, CanAlwaysTrue);
            HideShowSerie = new RelayCommand(HideShowSerieAction, CanAlwaysTrue);
            EditSerie = new RelayCommand(EditSerieAction, CanAlwaysTrue);
            DeleteSerie = new RelayCommand(DeleteSerieAction, CanAlwaysTrue);
            SortTableView = new RelayCommand(SortTableViewAction, CanAlwaysTrue);
            Copy = new RelayCommand(CopyAction, CanAlwaysTrue);
            ChartXDurationSet = new RelayCommand(ChartXDurationSetAction, CanAlwaysTrue);
            ChartXRangeSet = new RelayCommand(ChartXRangeSetAction, CanChartXRangeSet);
            ChartYRangeSet = new RelayCommand(ChartYRangeSetAction, CanChartYRangeSet);
            ChartShow = new RelayCommand(ChartShowAction, CanAlwaysTrue);
            ChartMoveToEnd = new RelayCommand(ChartMoveToEndAction, CanChartMoveToEnd);
            ImportSeries = new RelayCommand(ImportSeriesAction, CanAlwaysTrue);
            ExportSeries = new RelayCommand(ExportSeriesAction, CanAlwaysTrue);
            DeleteAllSeries = new RelayCommand(DeleteAllSeriesAction, CanAlwaysTrue);
            OpenArchive = new RelayCommand(OpenArchiveAction, CanAlwaysTrue);


            Series.CollectionChanged += Series_CollectionChanged;
           // suszarka = new Serie("Suszarka", 150);
            getValuesTimer = new System.Timers.Timer();
            getValuesTimer.Elapsed += GetValuesTimer_Elapsed;


          //  Series.Add(suszarka);

            plot = new PlotModel();
            
            DateTimeAxis dateTimeAxis = new DateTimeAxis {Position = AxisPosition.Bottom, StringFormat = "HH:mm:ss" };
            dateTimeAxis.AxisChanged += DateTimeAxis_AxisChanged;
            dateTimeAxis.MajorGridlineStyle = LineStyle.Solid;
            plot.Axes.Add(dateTimeAxis);
            plot.Axes[0].Reset();
            plot.Axes[0].Minimum = DateTimeAxis.ToDouble(DateTime.Now);
            plot.Axes[0].Maximum = DateTimeAxis.ToDouble(DateTime.Now.AddSeconds(Interval*10));
            //Console.WriteLine(DateTimeAxis.ToDateTime(plot.Axes[0].Minimum));
            //Console.WriteLine(DateTimeAxis.ToDateTime(plot.Axes[0].Maximum));


            LinearAxis valueAxis = new LinearAxis { Position = AxisPosition.Left };
            plot.Axes.Add(valueAxis);
            valueAxis.Reset();
            valueAxis.MajorGridlineStyle = LineStyle.Solid;
            //TODO 
            //Strzałka aby rozciągnąć Y na górę i dół na maksa


            //getValuesTimer.Interval = interval * 1000;
            //getValuesTimer.Enabled = true;




        }

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
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(Series.GetType());
                FileStream fs = new FileStream(path, FileMode.Open);
                ObservableCollection<Serie> seriesRead = (ObservableCollection<Serie>)serializer.Deserialize(fs);
                fs.Close();
                foreach (Serie s in seriesRead) Series.Add(s);
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
                    Plot.InvalidatePlot(true);
                }
            }
        }

      
            
        

        private void Plc_ConnectionStatusChanged(object sender, ConnectionStatusEnum e)
        {
            OnPropertyChanged("ConnectionStatus");
            OnPropertyChanged("ButtonConnectDisconnectContent");
            OnPropertyChanged("CanEditConnectionSetting");
        }

        private bool CanChartMoveToEnd(object obj)
        {
            if (!double.IsNaN(plot.Axes[0].DataMaximum))
            {
                if (DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(plot.Axes[0].ActualMaximum).AddSeconds(1)) < plot.Axes[0].DataMaximum) //0.00001 Granica błędu przy obliczeniach na małych liczbach
                    return true;
            }
            return false;
            
        }

        

        private void ChartMoveToEndAction(object obj)
        {
            TimeSpan timeSpan = DateTimeAxis.ToDateTime(plot.Axes[0].DataMaximum) - DateTimeAxis.ToDateTime(plot.Axes[0].ActualMaximum);
            double max = plot.Axes[0].ActualMaximum;
            double min = plot.Axes[0].ActualMinimum;
            plot.Axes[0].Reset();
            plot.Axes[0].PositionAtZeroCrossing = false;
            plot.Axes[0].Maximum = DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(max).Add(timeSpan));
            plot.Axes[0].Minimum = DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(min).Add(timeSpan));
            Plot.InvalidatePlot(true);
        }

        private void DateTimeAxis_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            //Strzałka w prawo inforumująca że wykres wychodzi poza zakres
            //Console.WriteLine("Oś się zmieniła " + e.ChangeType + " " + e.DeltaMaximum);
        }


        private void ChartShowAction(object obj)
        {
            bool chartFlowMemory = ChartFlow;
            ChartFlow = false;
            OnPropertyChanged("ChartFlow");
            plot.ResetAllAxes();
            plot.DefaultYAxis.PositionAtZeroCrossing = false;
            plot.Axes[0].PositionAtZeroCrossing = false;
            plot.Axes[0].Minimum = plot.Axes[0].DataMinimum;
            plot.Axes[0].Maximum = plot.Axes[0].DataMaximum;
            Plot.InvalidatePlot(true);
            ChartFlow = chartFlowMemory;
            OnPropertyChanged("ChartFlow");
        }



        private void ChartYRangeSetAction(object obj)
        {
            plot.DefaultYAxis.Reset();
            plot.DefaultYAxis.PositionAtZeroCrossing = false;
            plot.DefaultYAxis.Maximum = ChartYMax;
            plot.DefaultYAxis.Minimum = ChartYMin;
            Plot.InvalidatePlot(true);
        }



        private void ChartXRangeSetAction(object obj)
        {
            ChartFlow = false;
            OnPropertyChanged("ChartFlow");
            plot.Axes[0].Reset();
            plot.Axes[0].PositionAtZeroCrossing = false;
            plot.Axes[0].Maximum = DateTimeAxis.ToDouble(ChartDateXMax);
            plot.Axes[0].Minimum = DateTimeAxis.ToDouble(ChartDateXMin);
            Plot.InvalidatePlot(true);

        }

        private void ChartXDurationSetAction(object obj)
        {
            //TODO - done
            //bool saveChartFlow = ChartFlow;
            //ChartFlow = false;
            //OnPropertyChanged("ChartFlow");

            double max = plot.Axes[0].ActualMaximum;
            plot.Axes[0].Reset();
            plot.Axes[0].PositionAtZeroCrossing = false;
            //plot.Axes[0].Maximum = plot.Axes[0].DataMaximum;
            plot.Axes[0].Maximum = max;
            plot.Axes[0].Minimum = DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(max).AddSeconds(-(ChartXDurationValue)));
            //Console.WriteLine("Powinno ustawic na " + DateTimeAxis.ToDateTime(plot.Axes[0].Minimum) + " " + DateTimeAxis.ToDateTime(plot.Axes[0].Maximum));
            Plot.InvalidatePlot(true);

        }

        private void ChartMove()
        {
            try
            {
                //ChartFlow   ChartFlowOnEdge OverBorder  Move
                //1           1               1           1
                //1           1               0           0
                //1           0               1           1
                //1           0               0           1
                if (ChartFlow)
                {
                    if (!(ChartFlowOnEdge && !(plot.Axes[0].ActualMaximum <= plot.Axes[0].DataMaximum)))
                    {
                        TimeSpan timeSpan = DateTimeAxis.ToDateTime(plot.Axes[0].DataMaximum) - DateTimeAxis.ToDateTime(lastXAxisDataMaximum ?? plot.Axes[0].DataMaximum);
                        double max = plot.Axes[0].ActualMaximum;
                        double min = plot.Axes[0].ActualMinimum;
                        plot.Axes[0].Reset();
                        plot.Axes[0].PositionAtZeroCrossing = false;
                        plot.Axes[0].Maximum = DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(max).Add(timeSpan));
                        if (ChartFlowMinLock)
                        {
                            plot.Axes[0].Minimum = min;
                        }
                        else
                        {
                            plot.Axes[0].Minimum = DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(min).Add(timeSpan));
                        }
                    }
                }
                lastXAxisDataMaximum = plot.Axes[0].DataMaximum;
            }
            catch { }
            Plot.InvalidatePlot(true);

        }
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

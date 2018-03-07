using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Common;
using OmronPLCTemperatureReader.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class PlotViewModel : ViewModelBase
    {
        private PlotModel plot;
        private ObservableCollection<Serie> series;
        private ObservableCollection<RectangleAnnotation> connectionRefusedAreas = new ObservableCollection<RectangleAnnotation>();

        public PlotModel Plot
        {
            get
            {
                plot.Series.Clear();
                //Dla każdej serii utwórzyć Lineseries i do tego lineseries 
                for (int i = 0; i < series.Count; i++)
                {
                    Serie s = series[i];
                    LineSeries lineSeries = new LineSeries();
                    lineSeries.Title = s.Name;
                    lineSeries.IsVisible = s.Visibility;
                    lineSeries.MarkerType = MarkerType.Diamond;
                    lineSeries.MarkerSize = 2;
                    foreach (Models.KeyValuePair<DateTime, double> pair in s.Data) //TODO Modyfikacja kolekcji!!
                    {
                        lineSeries.Points.Add(DateTimeAxis.CreateDataPoint(pair.Key, pair.Value));
                    }
                    plot.Series.Add(lineSeries);
                }
                plot.Annotations.Clear();
                foreach (RectangleAnnotation ra in connectionRefusedAreas)
                {
                    plot.Annotations.Add(ra);
                }


                return plot;
            }

            set
            {
                plot = value;
                SetProperty(ref plot, value);
            }
        }

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
        public RelayCommand ChartXDurationSet { get; set; }
        public RelayCommand ChartXRangeSet { get; set; }
        public RelayCommand ChartYRangeSet { get; set; }
        public RelayCommand ChartShow { get; set; }
        public RelayCommand ChartMoveToEnd { get; set; }


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

        public string ChartTitle
        {
            get { return plot.Title; }
            set
            {
                plot.Title = value;
                Plot.InvalidatePlot(true);
            }
        }

        public bool ChartLegendIsVisible
        {
            get { return plot.IsLegendVisible; }
            set
            {
                plot.IsLegendVisible = value;
                Plot.InvalidatePlot(true);
            }
        }



        #endregion

        private bool CanChartMoveToEnd(object obj)
        {
            if (!double.IsNaN(plot.Axes[0].DataMaximum))
            {
                if (DateTimeAxis.ToDouble(DateTimeAxis.ToDateTime(plot.Axes[0].ActualMaximum).AddSeconds(1)) < plot.Axes[0].DataMaximum) //0.00001 Granica błędu przy obliczeniach na małych liczbach
                    return true;
            }
            return false;

        }


        public PlotViewModel(PlotModel plot, ObservableCollection<Serie> series)
        {

            ChartDateXMin = DateTime.Now;
            ChartDateXMax = DateTime.Now;
            ChartXDurationSet = new RelayCommand(ChartXDurationSetAction, true);
            ChartXRangeSet = new RelayCommand(ChartXRangeSetAction, CanChartXRangeSet);
            ChartYRangeSet = new RelayCommand(ChartYRangeSetAction, CanChartYRangeSet);
            ChartShow = new RelayCommand(ChartShowAction, true);
            ChartMoveToEnd = new RelayCommand(ChartMoveToEndAction, CanChartMoveToEnd);

            DateTimeAxis dateTimeAxis = new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "HH:mm:ss" };
            plot.Axes.Add(dateTimeAxis);
            plot.Axes[0].AxisChanged += DateTimeAxis_AxisChanged;
            plot.Axes[0].MajorGridlineStyle = LineStyle.Solid;

            plot.Axes[0].Reset();
            plot.Axes[0].Minimum = DateTimeAxis.ToDouble(DateTime.Now);
            plot.Axes[0].Maximum = DateTimeAxis.ToDouble(DateTime.Now.AddSeconds(30));
            //Console.WriteLine(DateTimeAxis.ToDateTime(plot.Axes[0].Minimum));
            //Console.WriteLine(DateTimeAxis.ToDateTime(plot.Axes[0].Maximum));

            LinearAxis valueAxis = new LinearAxis { Position = AxisPosition.Left };
            plot.Axes.Add(valueAxis);
            valueAxis.Reset();
            valueAxis.MajorGridlineStyle = LineStyle.Solid;

            this.plot = plot;
            this.series = series;
        }

        public void InvalidatePlot(bool updateData)
        {
            Plot.InvalidatePlot(updateData);
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


        //TODO dorobić ciągłe rysowanie jak jest łączenie
        public void ConnectionStatusChanged(object sender, ConnectionStatusChangedArgs e)
        {
            DateTime now = DateTime.Now;
            switch (e.Actual)
            {
                case ConnectionStatusEnum.CONNECTED:
                    Console.WriteLine("Połączono" + now);
                    if (connectionRefusedAreas.Count > 0)
                        connectionRefusedAreas.Last().MaximumX = DateTimeAxis.ToDouble(now);
                    break;
                case ConnectionStatusEnum.CONNECTION_LOST:
                case ConnectionStatusEnum.DISCONNECTING:
                    Console.WriteLine("Połączenie utracone " + now);
                    RectangleAnnotation ra = new RectangleAnnotation
                    {
                        MinimumX = DateTimeAxis.ToDouble(now),
                        MaximumX = DateTimeAxis.ToDouble(now),
                        Fill = OxyColor.FromAColor(80, OxyColors.Red)
                    };
                    connectionRefusedAreas.Add(ra);
                    break;
            }

        }

        private void ConnectionRefusedTimes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            //if (connectionRefusedTimes.Count % 2 == 1)
            //{
            //    DateTime now = (DateTime)e.NewItems[0];
            //    Console.WriteLine("Add");
            //    RectangleAnnotation ra = new RectangleAnnotation
            //    {
            //        MinimumX = DateTimeAxis.ToDouble(now),
            //        MaximumX = DateTimeAxis.ToDouble(now),
            //        Fill = OxyColor.FromAColor(80, OxyColors.Red)
            //    };
            //    connectionRefusedAreas.Add(ra);

            //}
            //else
            //{
            //    DateTime now = (DateTime)e.NewItems[0];
            //    Console.WriteLine("Replace");
            //    
            //}
            //Plot.InvalidatePlot(true);

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

        private double? lastXAxisDataMaximum;

        
        public void ChartMove()
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
            Refresh();

        }
    }
}

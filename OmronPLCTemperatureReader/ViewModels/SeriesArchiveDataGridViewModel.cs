using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Views;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class SeriesArchiveDataGridViewModel : SeriesDataGridViewModel
    {
        #region UI properties

        //public ObservableCollection<Serie> Series { get; set; }

        #endregion



        public SeriesArchiveDataGridViewModel(ViewModelBase parentViewModel, ObservableCollection<Serie> series): base(series) {
            //this.DeleteSerie = new RelayCommand(DeleteSerieAction);
            //this.HideShowSerie = new RelayCommand(HideShowSerieAction);
            //this.DeleteAllSeries = new RelayCommand(DeleteAllSeriesAction);
            this.ParentViewModel = parentViewModel;
            this.ImportSeries = new RelayCommand(ImportSeriesAction);
            this.OpenArchive = new RelayCommand(OpenArchiveAction);
            this.AddArchive = new RelayCommand(AddArchiveAction);
            this.Series = series;
        }

        private void AddArchiveAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Txt file (*.txt)|*.txt";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var filePath = Path.GetFullPath(openFileDialog.FileName);
                var openArchiveResult = this.ImportArchiveFromFile(filePath);

                if (openArchiveResult)
                {
                    MessageBox.Show("Wartości serii zaimportowane pomyślnie", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Nie udało się zaimportować wartości serii", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            Command("Plot.Refresh");
            Command("Table.Refresh");
        }

        private void OpenArchiveAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Txt file (*.txt)|*.txt";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var filePath = Path.GetFullPath(openFileDialog.FileName);
                Series.Clear();
                var openArchiveResult = this.ImportArchiveFromFile(filePath);

                if (openArchiveResult)
                {
                    MessageBox.Show("Wartości serii zaimportowane pomyślnie", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Nie udało się zaimportować wartości serii", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            Command("Plot.Refresh");
            Command("Table.Refresh");
            try
            {
                (ParentViewModel as MainWindowViewModel).PlotArchiveViewModel.ChartShowAction(null);
            }
            catch { };
        }

        private bool ImportArchiveFromFile(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    using (StreamReader file = new StreamReader(fs))
                    {
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                var lineSplitted = line.Split('\t');
                                var name = lineSplitted[0];
                                var dateTime = DateTime.Parse(lineSplitted[1]);
                                var value = double.Parse(lineSplitted[2]);
                                var serie = findOrCreateSerieByName(name);
                                var isDuplicate = serie.ExistDateTime(dateTime);
                                if (!isDuplicate)
                                {
                                    serie.Data.Add(new Models.KeyValuePair<DateTime, double>(dateTime, value));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        private Serie findOrCreateSerieByName(string name)
        {
            foreach (Serie s in Series)
            {
                if (s.Name.Equals(name))
                {
                    return s;
                }
            }
            var newSerie = new SerieArchive(name);
            Series.Add(newSerie);
            return newSerie;
        }


        //private SerieArchive selectedItem;
        //public SerieArchive SelectedItem
        //{
        //    get { return selectedItem; }
        //    set
        //    {
        //        selectedItem = value;
        //        OnPropertyChanged("ButtonHideShowSerieContent");
        //    }
        //}




        //public string FilePath { get; set; }


        //public RelayCommand HideShowSerie { get; set; }
        //public RelayCommand DeleteSerie { get; set; }
        //public RelayCommand DeleteAllSeries { get; set; }
        public RelayCommand ImportSeries { get; set; }
        public RelayCommand OpenArchive { get; set; }
        public RelayCommand AddArchive { get; set; }


        private void ImportSeriesAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Txt file (*.txt)|*.txt";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = Path.GetFullPath(openFileDialog.FileName);
                Command("Main.ExportXML");
            }
        }


        //private void HideShowSerieAction(object obj)
        //{
        //    SerieArchive selectedItem = obj as SerieArchive;
        //    if (selectedItem != null)
        //    {
        //        selectedItem.Visibility = !selectedItem.Visibility;
        //    }
        //    OnPropertyChanged("ButtonHideShowSerieContent");
        //    CollectionViewSource.GetDefaultView(Series).Refresh();
        //    Command("Table.Refresh");
        //    Command("Plot.Refresh");
        //}

        //private void DeleteSerieAction(object obj)
        //{
        //    SerieArchive selectedItem = obj as SerieArchive;
        //    if (selectedItem != null)
        //    {
        //        if (MessageBox.Show("Usunąć serię " + selectedItem.Name + "?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == (MessageBoxResult.Yes))
        //        {
        //            Series.Remove(selectedItem);
        //            CollectionViewSource.GetDefaultView(Series).Refresh();
        //            Command("Table.Refresh");
        //            Command("Plot.Refresh");
        //        }
        //    }
        //}

        //private void DeleteAllSeriesAction(object obj)
        //{
        //    if (MessageBox.Show("Usunąć wszystkie serie?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == (MessageBoxResult.Yes))
        //    {
        //        while (Series.Count > 0)
        //        {
        //            Series.Remove(Series[0]);
        //            CollectionViewSource.GetDefaultView(Series).Refresh();
        //            Command("Table.Refresh");
        //            Command("Plot.Refresh");
        //        }
        //    }
        //}



    }
}
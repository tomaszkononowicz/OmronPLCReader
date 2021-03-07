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
        public SeriesArchiveDataGridViewModel(ViewModelBase parentViewModel, ObservableCollection<Serie> series): base(series) {

            this.ParentViewModel = parentViewModel;
            this.OpenArchive = new RelayCommand(OpenArchiveAction);
            this.Series = series;
        }

        public RelayCommand OpenArchive { get; set; }

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
                    MessageBox.Show("Wartości serii zaimportowane pomyślnie", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
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
        
    }
}
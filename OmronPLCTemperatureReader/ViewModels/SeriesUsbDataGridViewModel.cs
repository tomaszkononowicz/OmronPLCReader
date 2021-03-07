using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Views;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class SeriesUsbDataGridViewModel : SeriesDataGridViewModel
    {
        public SeriesUsbDataGridViewModel(ViewModelBase parentViewModel, ObservableCollection<Serie> series): base(series) {
            this.ParentViewModel = parentViewModel;
            this.OpenLocalFileUsb = new RelayCommand(OpenLocalFileUsbAction);
            this.OpenTerminalFileUsb = new RelayCommand(OpenTerminalFileUsbAction);
            this.Series = series;
        }

        public bool ImportAsExistingSeries { get; set; }

        private void OpenTerminalFileUsbAction(object obj)
        {
            ConnectionTerminalViewModel connectionTerminalViewModel = (ParentViewModel as MainWindowViewModel).ConnectionTerminalViewModel;
            TerminalFileBrowserViewModel terminalFileBrowserViewModel = new TerminalFileBrowserViewModel(connectionTerminalViewModel.Ip, connectionTerminalViewModel.Port, connectionTerminalViewModel.Login, connectionTerminalViewModel.Password, Series, ImportAsExistingSeries, SeriesMapping);
            new TerminalFileBrowserWindow(terminalFileBrowserViewModel).ShowDialog();
            
            Command("Plot.Refresh");
            Command("Table.Refresh");
        }

        private void OpenLocalFileUsbAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt;*.csv)|*.txt; *.csv|All files (*.*)|*.*";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var filePath = Path.GetFullPath(openFileDialog.FileName);
                var openLocalFileUsbResult = this.ImportUsbFromFile(filePath);

                if (openLocalFileUsbResult)
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
                (ParentViewModel as MainWindowViewModel).PlotUsbViewModel.ChartShowAction(null);
            }
            catch { };
        }

        private bool ImportUsbFromFile(string filePath)
        {
            List<Serie> importedSeries = new List<Serie>();
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    using (StreamReader file = new StreamReader(fs))
                    {
                        string headerLine = file.ReadLine();
                        if (string.IsNullOrWhiteSpace(headerLine))
                        {
                            return false;
                        }
                        else
                        {
                            string[] serieNames = headerLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
                            for (int i = 1; i < serieNames.Length; i++)
                            {
                                importedSeries.Add(new Serie(serieNames[i], 1));
                            }
                        }

                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                string[] lineValues = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                                if (lineValues.Length > 0)
                                {
                                    var dateTime = DateTime.Parse(lineValues[0]);

                                    for (int i = 0; i < importedSeries.Count; i++)
                                    {
                                        if (i+1 >= lineValues.Length)
                                        {
                                            continue;
                                        }

                                        var value = double.Parse(lineValues[i+1]);

                                        importedSeries[i].Data.Add(new Models.KeyValuePair<DateTime, double>(dateTime, value));
                                    }
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

            if (ImportAsExistingSeries)
            {
                ApplySerieMapping(importedSeries);
            }

            Series.Clear();
            importedSeries.ForEach(x => Series.Add(x));

            return true;
        }

        public void ApplySerieMapping(List<Serie> importedSeries)
        {
            foreach (Serie importedSerie in importedSeries)
            {
                try
                {
                    var mappedSerie = SeriesMapping.Single(seriesMapping => importedSerie.Name.Equals(seriesMapping.Key));
                    importedSerie.Name = mappedSerie.Value.Name;
                    importedSerie.Visibility = mappedSerie.Value.Visibility;
                    importedSerie.Multiplier = mappedSerie.Value.Multiplier;
                }
                catch { }
            }
        }

        public RelayCommand OpenLocalFileUsb { get; set; }
        public RelayCommand OpenTerminalFileUsb { get; set; }
    }
}
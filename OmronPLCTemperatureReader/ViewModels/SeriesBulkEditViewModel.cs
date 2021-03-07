using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class SeriesBulkEditViewModel : ViewModelBase
    {

        #region UI properties

        public ObservableCollection<Serie> Series { get; set; }
        public List<System.Collections.Generic.KeyValuePair<string, Serie>> SeriesMapping { get; set; }
        private ObservableCollection<Serie> seriesOrigin { get; set; }

        public ObservableCollection<System.Collections.Generic.KeyValuePair<string, double>> Multipliers { get; set; }
 

        #endregion

        #region Commands properties and implementations

        public RelayCommand Save { get; set; }
        public RelayCommand Cancel { get; set; }
        public RelayCommand BulkImport { get; set; }
        public RelayCommand CheckedChange { get; set; }

        public bool? AllSelected 
        { 
            get
            {
                if (Series.All(serie => serie.Visibility)) {
                    return true;
                } 
                if (Series.Any(serie => serie.Visibility))
                {
                    return null;
                }
                return false;
            }
            set
            {
                if (value.HasValue)
                {
                    foreach (Serie serie in Series)
                    {
                        serie.Visibility = value.Value;
                    }
                }
            }
        }

        private bool CanCancel(object obj)
        {
            return true;
        }

        private void CancelAction(object obj)
        {
            Window window = obj as Window;
            window.DialogResult = false;
            window.Close();
        }

        private bool CanSave(object obj)
        {
            return true;
        }

        private void SaveAction(object obj)
        {
            ObservableCollection<Serie> series = ((obj as Window).DataContext as SeriesBulkEditViewModel).Series;


            seriesOrigin.Clear();

            foreach (var serie in series)
            {
                seriesOrigin.Add(serie);
            }


            Window window = obj as Window;
            window.DialogResult = true;
            window.Close();
        }

        private void BulkImportAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Txt file (*.txt)|*.txt";
            var result = openFileDialog.ShowDialog();
            
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var filePath = Path.GetFullPath(openFileDialog.FileName);
                var serieMapping = this.ImportSeriesSettingsFromFile(filePath);

                SeriesMapping.Clear();
                SeriesMapping.AddRange(serieMapping);

                if (serieMapping.Any())
                {
                    var openLocalFileUsbResult = this.ApplySerieMapping(serieMapping);
                    if (openLocalFileUsbResult)
                    {
                        MessageBox.Show("Ustawienia serii zaimportowane pomyślnie", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Nie udało się zaimportować ustawień serii", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Nie udało się odczytać żadnych ustawień serii", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void CheckedChangeAction(object obj)
        {
            OnPropertyChanged(nameof(AllSelected));
        }

        private System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Serie>> ImportSeriesSettingsFromFile(string filePath)
        {
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Serie>> serieMapping = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Serie>>();
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
                                string[] lineValues = line.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                                if (lineValues.Count() >= 4)
                                {
                                    var oldSerieName = lineValues[0].Trim();
                                    var newSerieName = lineValues[1].Trim();
                                    var newVisibility = !(lineValues[2].Trim().Equals("0"));

                                    if (double.TryParse(lineValues[3].Trim(), out double newMultiplier))
                                    {
                                        var serieMap = new System.Collections.Generic.KeyValuePair<string, Serie>(oldSerieName, new Serie(newSerieName, newVisibility, newMultiplier));

                                        serieMapping.Add(serieMap);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return serieMapping;
        }

        public bool ApplySerieMapping(List<System.Collections.Generic.KeyValuePair<string, Serie>> serieMapping)
        {
            foreach (var serieMap in serieMapping)
            {
                var serieToEdit = Series.FirstOrDefault(serie => string.Equals(serie.Name, serieMap.Key, StringComparison.InvariantCulture));
                if (serieToEdit != null)
                {
                    serieToEdit.Name = serieMap.Value.Name;
                    serieToEdit.Visibility = serieMap.Value.Visibility;
                    serieToEdit.Multiplier = serieMap.Value.Multiplier;
                }
            }

            return true;
        }


        #endregion


        public string WindowTitle { get; set; }

        public SeriesBulkEditViewModel(ObservableCollection<Serie> series, List<System.Collections.Generic.KeyValuePair<string, Serie>> seriesMapping)
        {
            seriesOrigin = series;
            SeriesMapping = seriesMapping;

            Series = new ObservableCollection<Serie>();
                      

            foreach (var serie in series)
            {
                Series.Add((Serie)serie.Clone());
            }


            Multipliers = new ObservableCollection<System.Collections.Generic.KeyValuePair<string, double>>()
            {
                new System.Collections.Generic.KeyValuePair<string, double>("1000", 1000),
                new System.Collections.Generic.KeyValuePair<string, double>("100", 100),
                new System.Collections.Generic.KeyValuePair<string, double>("10", 10),
                new System.Collections.Generic.KeyValuePair<string, double>("1 (Brak mnożnika)", 1),
                new System.Collections.Generic.KeyValuePair<string, double>("0,1", 0.1),
                new System.Collections.Generic.KeyValuePair<string, double>("0,01", 0.01),
                new System.Collections.Generic.KeyValuePair<string, double>("0,001", 0.001)
            };

            Save = new RelayCommand(SaveAction, CanSave);
            Cancel = new RelayCommand(CancelAction, CanCancel);
            BulkImport = new RelayCommand(BulkImportAction);
            CheckedChange = new RelayCommand(CheckedChangeAction);
        }
    }
}

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
    public class SeriesOnlineDataGridModelView : SeriesDataGridViewModel
    {
        #region UI properties

        //public ObservableCollection<Serie> Series { get; set; }

        #endregion


        public SeriesOnlineDataGridModelView(ViewModelBase parentViewModel, ObservableCollection<Serie> series) : base(series) {
            this.ParentViewModel = parentViewModel;
            AddSerie = new RelayCommand(AddSerieAction);
            EditSerie = new RelayCommand(EditSerieAction);
            ImportSeries = new RelayCommand(ImportSeriesAction);
            ExportSeries = new RelayCommand(ExportSeriesAction);
            ExportValues = new RelayCommand(ExportValuesAction);

            Series = series;
        }

        public RelayCommand AddSerie { get; set; }
        public RelayCommand EditSerie { get; set; }
        //public RelayCommand HideShowSerie { get; set; }
        //public RelayCommand DeleteSerie { get; set; }
        //public RelayCommand DeleteAllSeries { get; set; }

        public RelayCommand ImportSeries { get; set; }
        public RelayCommand ExportSeries { get; set; }
        public RelayCommand ExportValues { get; set; }
        public RelayCommand OpenArchive { get; set; }


        //private SerieOnline selectedItem;
        //public SerieOnline SelectedItem
        //{
        //    get { return selectedItem; }
        //    set
        //    {
        //        selectedItem = value;
        //        OnPropertyChanged("ButtonHideShowSerieContent");
        //    }
        //}




        //public string FilePath { get; set; }


        private void AddSerieAction(object obj)
        {
            SerieOnline newSerie = new SerieOnline("Nowa seria", 0);
            Nullable<bool> dialogResult = new AddEditSerieWindow(ref newSerie).ShowDialog();
            if (dialogResult == true)
            {
                Series.Add(newSerie);
            }
            Command("Table.Refresh");
            Command("Plot.Refresh");
        }



        private void EditSerieAction(object obj)
        {
            SerieOnline selectedItem = obj as SerieOnline;
            if (selectedItem != null)
            {
                new AddEditSerieWindow(ref selectedItem).ShowDialog();
                CollectionViewSource.GetDefaultView(Series).Refresh();
                Command("Table.Refresh");
                Command("Plot.Refresh");
            }
        }

        //private void DeleteSerieAction(object obj)
        //{
        //    SerieOnline selectedItem = obj as SerieOnline;
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




        private void ExportSeriesAction(object obj)
        {
            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "Xml file (*.xml)|*.xml";
            var result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = Path.GetFullPath(saveFileDialog.FileName);
                try
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(Series.GetType());
                    FileStream fs = new FileStream(FilePath, FileMode.Create);
                    serializer.Serialize(fs, Series);
                    //IP in last line as comment
                    var mainWindowViewModel = (this.ParentViewModel as MainWindowViewModel);
                    byte[] connectionInfo = Encoding.ASCII.GetBytes("<!--|" + mainWindowViewModel.ConnectionViewModel.Ip + "|" + mainWindowViewModel.ConnectionViewModel.Port + "|-->");
                    fs.Write(connectionInfo, 0, connectionInfo.Length);
                    fs.Close();
                    MessageBox.Show("Serie wyeksportowane pomyślnie", "Eksport", MessageBoxButton.OK);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Nie udało się wyeksportować serii \n{e.Message}", "Eksport", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        
        private void ImportSeriesAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Xml file (*.xml)|*.xml";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var filePath = Path.GetFullPath(openFileDialog.FileName);
                var importSeriesResult = this.ImportSeriesFromFile(filePath);
                var importConnectionResult = this.ImportConnectionFromFile(filePath);

                if (importSeriesResult && importConnectionResult)
                {
                    MessageBox.Show("Serie oraz informacje o połączeniu zaimportowane pomyślnie", "Import", MessageBoxButton.OK);
                }
                else if (importSeriesResult)
                {
                    MessageBox.Show("Nie udało się zaimportować informacji o połączeniu\n"
                + "Serie zaimportowane pomyślnie", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (importConnectionResult)
                {
                    MessageBox.Show("Nie udało się zaimportować serii\n"
                + "Informacje o połączeniu zaimportowane pomyślnie", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Nie udało się zaimportować serii oraz informacji o połączeniu", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public bool ImportSeriesFromFile(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(Series.GetType());
                    var seriesRead = (ObservableCollection<Serie>)serializer.Deserialize(fs);
                    foreach (SerieOnline s in seriesRead) Series.Add(s);
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public bool ImportConnectionFromFile(string filePath)
        {
            try
            {
                var connectionViewModel = (this.ParentViewModel as MainWindowViewModel).ConnectionViewModel;
                if (connectionViewModel.CanEditConnectionSetting)
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        fs.Position = Math.Max(0, fs.Length - 30);
                        byte[] connectionBytes = new byte[30];
                        fs.Read(connectionBytes, 0, 30);
                        var connectionString = Encoding.ASCII.GetString(connectionBytes);

                        if (connectionViewModel.CanEditConnectionSetting)
                        {
                            string ip;
                            string port;
                            string[] splitted = connectionString.Split('|');
                            ip = splitted[1];
                            port = splitted[2];

                            connectionViewModel.Ip = IPAddress.Parse(ip);
                            connectionViewModel.Port = ushort.Parse(port);
                            connectionViewModel.Refresh();
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        private void ExportValuesAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.SaveFileDialog();
            openFileDialog.Filter = "Txt file (*.txt)|*.txt";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var filePath = Path.GetFullPath(openFileDialog.FileName);
                var exportValues = ExportValuesToFile(filePath);

                if (exportValues)
                {
                    MessageBox.Show("Wartości Serii wyeksportowane pomyślnie", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Nie udało się wyeksportować wartości Serii", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool ExportValuesToFile(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (SerieOnline serie in Series)
                    {
                        foreach (var pair in serie.Data)
                        {
                            sb.Clear();
                            var lineToWrite = sb.Append(serie.Name).Append("\t").Append(pair.Key).Append("\t").Append(pair.Value * serie.Multiplier).Append("\r\n").ToString();
                            byte[] bytesToWrite = new UTF8Encoding(true).GetBytes(lineToWrite);
                            fs.Write(bytesToWrite, 0, bytesToWrite.Length);
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




    }
}
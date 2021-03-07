using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class TerminalFileBrowserViewModel : ViewModelBase
    {
        public ObservableCollection<TerminalFile> TerminalFiles { get; set; }
        public TerminalFile SelectedTerminalFile { get; set; }
        public RelayCommand Cancel { get; set; }
        public RelayCommand Import { get; set; }
        public RelayCommand RefreshFiles { get; set; }
        public RelayCommand Abort { get; set; }
        public RelayCommand TreeViewSelectItemCommand { get; set; }

        private TerminalService terminalService;

        private bool isConnecting;
        public bool IsConnecting
        {
            get { return isConnecting; }
            set
            {
                isConnecting = value;
                SetProperty(ref isConnecting, value);
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool ImportAsExistingSeries { get; set; }

        public List<System.Collections.Generic.KeyValuePair<string, Serie>> SeriesMapping { get; set; }

        public ObservableCollection<Serie> Series { get; set; }

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

        private void TreeViewSelectItemCommandAction(object obj)
        {
            TerminalFile terminalFile = obj as TerminalFile;
            SelectedTerminalFile = terminalFile;
        }

        private bool CanImport(object obj)
        {
            return (this.SelectedTerminalFile != null && !this.SelectedTerminalFile.IsDirectory);
        }

        private bool CanRefresh(object obj)
        {
            return (!this.IsConnecting);
        }

        private bool CanAbort(object obj)
        {
            return (this.IsConnecting);
        }
        public Visibility AbortVisibility
        {
            get
            {
                if (this.IsConnecting)
                {
                    return Visibility.Visible;
                }
                return Visibility.Hidden;
            }
        }


        private async void RefreshFilesAction(object obj)
        {
            GetTerminalFilesTreeAsync();
        }

        private void AbortAction(object obj)
        {
            this.terminalService.Cancel();
        }

        private async void ImportAction(object obj)
        {
            string fileContent = await this.terminalService.GetFileContentAsync(this.SelectedTerminalFile.FullPath);

            var openTerminalFileUsbResult = this.ImportUsbFromString(fileContent);

            if (openTerminalFileUsbResult)
            {
                Window window = obj as Window;
                window.DialogResult = true;
                window.Close();
                MessageBox.Show("Wartości serii zaimportowane pomyślnie", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Nie udało się zaimportować wartości serii", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            Command("Plot.Refresh");
            Command("Table.Refresh");
        }

        private bool ImportUsbFromString(string fileContent)
        {
            List<Serie> importedSeries = new List<Serie>();
            string[] fileLines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string headerLine = fileLines[0];
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return false;
            }
            else
            {
                string[] serieNames = headerLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < serieNames.Length; i++)
                {
                    importedSeries.Add(new Serie(serieNames[i]));
                }
            }

            for (int lineIndex = 1; lineIndex < fileLines.Length; lineIndex++)
            {
                string line = fileLines[lineIndex];
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] lineValues = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (lineValues.Length > 0)
                    {
                        DateTime dateTime;
                        try
                        {
                            dateTime = DateTime.Parse(lineValues[0]);
                        }
                        catch
                        {
                            MessageBox.Show($"Wartość '{lineValues[0]}' nie jest datą", "Terminal - Import", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        for (int i = 0; i < importedSeries.Count; i++)
                        {
                            if (i + 1 >= lineValues.Length)
                            {
                                continue;
                            }

                            var value = double.Parse(lineValues[i + 1]);

                            importedSeries[i].Data.Add(new Models.KeyValuePair<DateTime, double>(dateTime, value));
                        }
                    }
                }
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

        private async void GetTerminalFilesTreeAsync()
        {
            IsConnecting = true;
            this.TerminalFiles.Clear();
            TerminalFile terminalFile = await Task.Run(() => this.terminalService.GetTerminalFilesTreeAsync());
            IsConnecting = false;
            if (terminalFile != null)
            {
                this.TerminalFiles.Add(terminalFile);
            }
        }

        public TerminalFileBrowserViewModel(IPAddress ip, ushort port, string login, string password, ObservableCollection<Serie> series, bool importAsExistingSeries,  List<System.Collections.Generic.KeyValuePair<string, Serie>> seriesMapping)
        {
            Series = series;
            TerminalFiles = new ObservableCollection<TerminalFile>();
            this.ImportAsExistingSeries = importAsExistingSeries;
            this.SeriesMapping = seriesMapping;

            Cancel = new RelayCommand(CancelAction, CanCancel);
            Import = new RelayCommand(ImportAction, CanImport);
            RefreshFiles = new RelayCommand(RefreshFilesAction, CanRefresh);
            Abort = new RelayCommand(AbortAction, CanAbort);
            TreeViewSelectItemCommand = new RelayCommand(TreeViewSelectItemCommandAction);

            this.terminalService = new TerminalService(ip, port, login, password);
            GetTerminalFilesTreeAsync();
        }
    }
}

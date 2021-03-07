using OmronPLCTemperatureReader.Commands;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {

        #region UI properties
        private string seriesFilePath;
        public string SeriesFilePath
        {
            get { return seriesFilePath; }
            set
            {
                seriesFilePath = value;
                SetProperty(ref seriesFilePath, value);
                OnPropertyChanged();
            }
        }

        private string logsFolderPath;
        public string LogsFolderPath
        {
            get { return logsFolderPath; }
            set
            {
                logsFolderPath = value;
                SetProperty(ref logsFolderPath, value);
                OnPropertyChanged();
            }
        }

        private string logsFilePrefix;
        public string LogsFilePrefix
        {
            get { return logsFilePrefix; }
            set
            {
                logsFilePrefix = value;
                SetProperty(ref logsFilePrefix, value);
                OnPropertyChanged();
            }
        }

        private int terminalConnectionTimeoutSeconds;
        public int TerminalConnectionTimeoutSeconds
        {
            get { return terminalConnectionTimeoutSeconds; }
            set
            {
                terminalConnectionTimeoutSeconds = value;
                SetProperty(ref terminalConnectionTimeoutSeconds, value);
                OnPropertyChanged();
            }
        }

        private int pingTimeoutMiliseconds;
        public int PingTimeoutMiliseconds
        {
            get { return pingTimeoutMiliseconds; }
            set
            {
                pingTimeoutMiliseconds = value;
                SetProperty(ref pingTimeoutMiliseconds, value);
                OnPropertyChanged();
            }
        }



        #endregion

        #region Commands properties and implementations

        public RelayCommand Save { get; set; }
        public RelayCommand Cancel { get; set; }

        public RelayCommand BrowseSeriesFilePath { get; set; }
        public RelayCommand BrowseLogsFolderPath { get; set; }
        public RelayCommand ClearSeriesFilePath { get; set; }
        public RelayCommand GoToSeriesFilePath { get; set; }
        public RelayCommand ClearLogsFolderPath { get; set; }
        public RelayCommand GoToLogsFolderPath { get; set; }



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
            Properties.Settings.Default.SeriesFilePath = SeriesFilePath;
            Properties.Settings.Default.LogsFolderPath = LogsFolderPath;
            Properties.Settings.Default.LogsFilePrefix = LogsFilePrefix;
            Properties.Settings.Default.TerminalConnectionTimeoutSeconds = TerminalConnectionTimeoutSeconds;
            Properties.Settings.Default.PingTimeoutMiliseconds = PingTimeoutMiliseconds;
            Properties.Settings.Default.Save();
            Window window = obj as Window;
            window.DialogResult = true;
            window.Close();
        }


        #endregion


        public string WindowTitle { get; set; }
        public SettingsViewModel(ViewModelBase parentViewModel)
        {
            this.ParentViewModel = parentViewModel;
            WindowTitle = "Ustawienia";

            SeriesFilePath = Properties.Settings.Default.SeriesFilePath;
            LogsFolderPath = Properties.Settings.Default.LogsFolderPath;
            LogsFilePrefix = Properties.Settings.Default.LogsFilePrefix;
            TerminalConnectionTimeoutSeconds = Properties.Settings.Default.TerminalConnectionTimeoutSeconds;
            PingTimeoutMiliseconds = Properties.Settings.Default.PingTimeoutMiliseconds;

            Save = new RelayCommand(SaveAction, CanSave);
            Cancel = new RelayCommand(CancelAction, CanCancel);
            BrowseSeriesFilePath = new RelayCommand(BrowseSeriesFilePathAction);
            BrowseLogsFolderPath = new RelayCommand(BrowseLogsFolderPathAction);
            ClearSeriesFilePath = new RelayCommand(ClearSeriesFilePathAction);
            GoToSeriesFilePath = new RelayCommand(GoToSeriesFilePathAction);
            ClearLogsFolderPath = new RelayCommand(ClearLogsFolderPathAction);
            GoToLogsFolderPath = new RelayCommand(GoToLogsFolderPathAction);
        }

        private void ClearSeriesFilePathAction(object obj)
        {
            SeriesFilePath = string.Empty;
        }

        private void GoToSeriesFilePathAction(object obj)
        {
            if (!string.IsNullOrWhiteSpace(SeriesFilePath))
            {
                OpenInExplorer(Path.GetDirectoryName(SeriesFilePath));
            }
        }

        private void ClearLogsFolderPathAction(object obj)
        {
            LogsFolderPath = string.Empty;
        }

        private void GoToLogsFolderPathAction(object obj)
        {
            OpenInExplorer(LogsFolderPath);
        }

        private bool OpenInExplorer(string pathToDirectory)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(pathToDirectory))
                {
                    Process.Start(pathToDirectory);
                    return true;
                }
                return false;
            }
            catch
            {
                MessageBox.Show("Nie można otowrzyć podanej lokalizacji", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void BrowseSeriesFilePathAction(object obj)
        {
            using (var openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.Filter = "Xml file (*.xml)|*.xml";
                var result = openFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    SeriesFilePath = Path.GetFullPath(openFileDialog.FileName);
                }
            }
        }
        private void BrowseLogsFolderPathAction(object obj)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    LogsFolderPath = Path.GetFullPath(dialog.SelectedPath);
                }
            }
        }
    }
}

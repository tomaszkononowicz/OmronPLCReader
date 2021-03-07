using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class SeriesDataGridViewModel : ViewModelBase
    {
        #region UI properties




        protected Serie selectedItem;
        public Serie SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                OnPropertyChanged("ButtonHideShowSerieContent");
            }
        }



        #endregion

        public ObservableCollection<Serie> Series { get; set; }
        public List<System.Collections.Generic.KeyValuePair<string, Serie>> SeriesMapping { get; set; }

        public SeriesDataGridViewModel(ObservableCollection<Serie> series)
        {
            this.DeleteSerie = new RelayCommand(DeleteSerieAction);
            this.HideShowSerie = new RelayCommand(HideShowSerieAction);
            this.DeleteAllSeries = new RelayCommand(DeleteAllSeriesAction);
            this.EditAllSeries = new RelayCommand(EditAllSeriesAction);
            this.Series = series;
            this.SeriesMapping = new List<System.Collections.Generic.KeyValuePair<string, Serie>>();
        }

        public string FilePath { get; set; }


        public RelayCommand HideShowSerie { get; set; }
        public RelayCommand DeleteSerie { get; set; }
        public RelayCommand DeleteAllSeries { get; set; }
        public RelayCommand EditAllSeries { get; set; }



        private void HideShowSerieAction(object obj)
        {
            Serie selectedItem = obj as Serie;
            if (selectedItem != null)
            {
                selectedItem.Visibility = !selectedItem.Visibility;
            }
            OnPropertyChanged("ButtonHideShowSerieContent");
            CollectionViewSource.GetDefaultView(Series).Refresh();
            Command("Table.Refresh");
            Command("Plot.Refresh");
        }


        private void DeleteSerieAction(object obj)
        {
            Serie selectedItem = obj as Serie;
            if (selectedItem != null)
            {
                if (MessageBox.Show("Usunąć serię " + selectedItem.Name + "?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning) == (MessageBoxResult.Yes))
                {
                    Series.Remove(selectedItem);
                    CollectionViewSource.GetDefaultView(Series).Refresh();
                    Command("Table.Refresh");
                    Command("Plot.Refresh");
                }
            }
        }





        private void DeleteAllSeriesAction(object obj)
        {
            if (MessageBox.Show("Usunąć wszystkie serie?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning) == (MessageBoxResult.Yes))
            {
                while (Series.Count > 0)
                {
                    Series.Remove(Series[0]);
                    CollectionViewSource.GetDefaultView(Series).Refresh();
                    Command("Table.Refresh");
                    Command("Plot.Refresh");
                }
            }
        }

        private void EditAllSeriesAction(object obj)
        {
            SeriesBulkEditViewModel seriesBulkEditViewModel = new SeriesBulkEditViewModel(Series, SeriesMapping);
            new SeriesBulkEditWindow(seriesBulkEditViewModel).ShowDialog();
            CollectionViewSource.GetDefaultView(Series).Refresh();
            Command("Table.Refresh");
            Command("Plot.Refresh");
        }




    }
}
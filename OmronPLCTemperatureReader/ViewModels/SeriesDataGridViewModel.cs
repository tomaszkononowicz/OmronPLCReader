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

        public SeriesDataGridViewModel(ObservableCollection<Serie> series)
        {
            this.DeleteSerie = new RelayCommand(DeleteSerieAction);
            this.HideShowSerie = new RelayCommand(HideShowSerieAction);
            this.DeleteAllSeries = new RelayCommand(DeleteAllSeriesAction);
            this.Series = series;
        }

        public string FilePath { get; set; }
        private Visibility visibility;
        public Visibility Visibility {
            get { return visibility; }
            set
            {
                visibility = value;
                OnPropertyChanged("Visibility");
            }
        }



        public RelayCommand HideShowSerie { get; set; }
        public RelayCommand DeleteSerie { get; set; }
        public RelayCommand DeleteAllSeries { get; set; }



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
                if (MessageBox.Show("Usunąć serię " + selectedItem.Name + "?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == (MessageBoxResult.Yes))
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
            if (MessageBox.Show("Usunąć wszystkie serie?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == (MessageBoxResult.Yes))
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




    }
}
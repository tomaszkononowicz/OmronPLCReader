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

        protected ObservableCollection<Serie> series;

        public SeriesDataGridViewModel(ObservableCollection<Serie> series)
        {
            this.series = series;
        }

        public string FilePath { get; private set; }




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
            CollectionViewSource.GetDefaultView(series).Refresh();
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
                    series.Remove(selectedItem);
                    CollectionViewSource.GetDefaultView(series).Refresh();
                    Command("Table.Refresh");
                    Command("Plot.Refresh");
                }
            }
        }





        private void DeleteAllSeriesAction(object obj)
        {
            if (MessageBox.Show("Usunąć wszystkie serie?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == (MessageBoxResult.Yes))
            {
                while (series.Count > 0)
                {
                    series.Remove(series[0]);
                    CollectionViewSource.GetDefaultView(series).Refresh();
                    Command("Table.Refresh");
                    Command("Plot.Refresh");
                }
            }
        }




    }
}
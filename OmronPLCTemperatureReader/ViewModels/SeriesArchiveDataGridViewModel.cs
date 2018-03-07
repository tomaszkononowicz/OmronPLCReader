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

        public SeriesArchiveDataGridViewModel(ObservableCollection<Serie> series) : base(series) { }

        public string FilePath { get; private set; }

        public RelayCommand ImportSeries { get; set; }

     
        private void ImportSeriesAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Xml file (*.txt)|*.txt";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = Path.GetFullPath(openFileDialog.FileName);
                Command("Main.ExportXML");
            }
        }


    }
}
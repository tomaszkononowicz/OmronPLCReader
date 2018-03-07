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
    public class SeriesOnlineDataGridModelView : SeriesDataGridViewModel
    {
        #region UI properties




        private Serie selectedItem;
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


        public SeriesOnlineDataGridModelView(ObservableCollection<Serie> series) : base(series) { }


        public string FilePath { get; private set; }



        public RelayCommand AddSerie { get; set; }
        public RelayCommand EditSerie { get; set; }


        public RelayCommand ImportSeries { get; set; }
        public RelayCommand ExportSeries { get; set; }
        public RelayCommand OpenArchive { get; set; }


        private void AddSerieAction(object obj)
        {
            SerieOnline newSerie = new SerieOnline("Nowa seria", 0);
            Nullable<bool> dialogResult = new AddEditSerieWindow(ref newSerie).ShowDialog();
            if (dialogResult == true)
            {
                series.Add(newSerie);
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
                CollectionViewSource.GetDefaultView(series).Refresh();
                Command("Table.Refresh");
                Command("Plot.Refresh");

            }
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




        private void ExportSeriesAction(object obj)
        {
            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "Xml file (*.xml)|*.xml";
            var result = saveFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = Path.GetFullPath(saveFileDialog.FileName);
                Command("Main.ExportXML");
            }
        }

        
        private void ImportSeriesAction(object obj)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Xml file (*.xml)|*.xml";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = Path.GetFullPath(openFileDialog.FileName);
                Command("Main.ExportXML");
            }
        }






    }
}
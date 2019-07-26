using OmronPLCTemperatureReader.Commands;
using OmronPLCTemperatureReader.Models;
using OxyPlot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OmronPLCTemperatureReader.ViewModels
{
    public class TableViewModel : ViewModelBase
    {

        private ObservableCollection<Serie> series;

        public TableViewModel(ViewModelBase parentViewModel, ObservableCollection<Serie> series)
        {
            this.ParentViewModel = parentViewModel;
            SortTableView = new RelayCommand(SortTableViewAction);
            Copy = new RelayCommand(CopyAction);
            Delete = new RelayCommand(DeleteAction);
            this.series = series;
            
        }



        private bool? ascending;
        private string sortKey;
        public IEnumerable TableView
        {
            get
            {
                var list = (from s in series
                            from d in s.Data
                            where s.Visibility == true
                            select new SerieTable (s.Name, d.Key, d.Value)).ToList();
                if (ascending == true)
                {
                    return list.OrderBy((x) =>
                    {
                        try
                        {
                            return x.GetType().GetProperty(sortKey).GetValue(x, null);
                        }
                        catch
                        {
                            return null;
                        }
                    });
                }
                if (ascending == false)
                {
                    return list.OrderByDescending((x) =>
                    {
                        try
                        {
                            return x.GetType().GetProperty(sortKey).GetValue(x, null);
                        }
                        catch
                        {
                            return null;
                        }
                    });
                }
                return list;
            }
        }

        private object selectedItemTableView;
        public object SelectedItemTableView
        {
            get { return selectedItemTableView; }
            set
            {
                selectedItemTableView = value;
            }
        }


        public RelayCommand SortTableView { get; set; }
        public RelayCommand Copy { get; set; }
        public RelayCommand Delete { get; set; }

        private void CopyAction(object obj)
        {
            StringBuilder sb = new StringBuilder();
            IEnumerable<SerieTable> items = obj as IEnumerable<SerieTable>;
            foreach (var i in items)
            {
                sb.Append(i.Name).Append("\t").Append(i.DateTime).Append("\t").Append(i.Value).Append("\r\n");
            }
            Clipboard.SetText(sb.ToString());
        }

        private void DeleteAction(object obj)
        {
            IEnumerable<SerieTable> items = obj as IEnumerable<SerieTable>;
            foreach (var i in items)
            {
                foreach (Serie s in series)
                {
                    s.delete(s.findByDateTimeAndValue(i.DateTime, i.Value));
                }
            }
            OnPropertyChanged("TableView");
            Command("Plot.Refresh");
        }


        private void SortTableViewAction(object obj)
        {
            sortKey = sortKey ?? "";
            if (ascending == null || !sortKey.Equals(obj)) ascending = true;
            else if (ascending == true) ascending = false;
            else ascending = null;
            sortKey = obj as string;
            OnPropertyChanged("TableView");
        }

    }
}

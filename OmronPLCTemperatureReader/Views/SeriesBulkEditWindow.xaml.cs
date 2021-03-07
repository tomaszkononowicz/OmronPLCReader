using OmronPLCTemperatureReader.Models;
using OmronPLCTemperatureReader.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OmronPLCTemperatureReader.Views
{
    /// <summary>
    /// Interaction logic for AddEditSerieWindow.xaml
    /// </summary>
    public partial class SeriesBulkEditWindow : Window
    {
        public SeriesBulkEditWindow(SeriesBulkEditViewModel seriesBulkEditViewModel)
        {
            InitializeComponent();
            DataContext = seriesBulkEditViewModel;
           

        }
    }

}

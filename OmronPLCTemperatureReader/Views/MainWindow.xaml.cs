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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OmronPLCTemperatureReader.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {     
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();    
               
        }

        ///
        /// TODO: Lista co poprawić po ukończeniu prjektu
        /// Zrzucanie do pliku danych
        /// Normalne uruchamianie nowego okna
        /// Ile może być maks DMów (65535?)
        /// Tooltipy np przy mnożniku
        /// 
        /// 194.187.238.5
        /// Port 9600
        /// DM 2068
        ///


    }
}

using OmronPLCTemperatureReader.Services;
using OmronPLCTemperatureReader.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        /// W propercjach jeżeli private nie potrzebne to usunac
        /// VuewModelBase Iproperty rpzerzucić do modelu
        /// DM konfiguracyjny np sprawdzający licencję
        /// Usuwanie gdy za dużo 

        /// wpis godzina wartosc seria></wpis>
        /// https://www.codeproject.com/Tips/443588/Simple-Csharp-FTP-Class
        /*

        będzie można otworzyć wiele plików
        Pokaże się się nazwa Serii i ile wpisów ma dana seria początek i koniec i będzie można scalić
            Przeskanowanie całego pliku i na podstawie

        Zapisywanie przeglądania z opcją utworzenia nowego pliku lub odtworzenia akcji z tamtego pliku*/


    }
}

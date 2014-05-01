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
using System.Reflection;
using System.Runtime.InteropServices;

namespace Credit.PointMall.Scraper
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window, IMainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainWindowViewModel();
            this.webBrowser.SetSilent(true);
       }

        public WebBrowser WebBrowser
        {
            get { return this.webBrowser; }
        }

        public PasswordBox PasswordBox
        {
            get { return this.passwordBox; }
        }

        private async void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = (PasswordBox)sender;

            await Task.Run(() =>
            {
                Properties.Settings.Default.Password = passwordBox.Password;
                Properties.Settings.Default.Save();
            });
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.passwordBox.Password = Properties.Settings.Default.Password;
        }
    }
}

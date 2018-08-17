using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Xceed.Wpf.Toolkit;

namespace ImportCerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BusyIndicator Indicator
        {
            get
            {
                return this.ActivityIndicator;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            var client = CreateKeyValueClient();
        }

        private static KeyVaultClient CreateKeyValueClient()
        {
            var clientId = Config.GetValue<string>("AzureVaultAdClientId");
            var clientSecret = Config.GetValue<string>("AzureVaultAdClientSecret");

            var keyClient = new KeyVaultClient(async (authority, resource, scope) =>
            {
                var adCredential = new ClientCredential(clientId, clientSecret);
                var authenticationContext = new AuthenticationContext(authority, null);
                return (await authenticationContext.AcquireTokenAsync(resource, adCredential).ConfigureAwait(false)).AccessToken;
            });

            return keyClient;
        }

        private void MenuOption_Click(object sender, RoutedEventArgs e)
        {
            var control = ((Button)e.OriginalSource).Tag;

            this.CreateControl.Visibility = Visibility.Collapsed;
            this.ImportContol.Visibility = Visibility.Collapsed;

            switch (control)
            {
                case "CreateControl":
                    this.CreateControl.Visibility = Visibility.Visible;
                    break;
                case "ImportControl":
                    this.ImportContol.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = new Hyperlink();
            link.NavigateUri = new Uri("http://andres.im", UriKind.Absolute);
            link.Inlines.Add("andres.im");
            link.RequestNavigate += (s, ev) =>
            {
                Process.Start(new ProcessStartInfo(ev.Uri.AbsoluteUri));
                ev.Handled = true;
            };

            var window = new Window();
            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
            stackPanel.Children.Add(new Label { Content = link, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            window.Content = stackPanel;
            window.ResizeMode = ResizeMode.NoResize;
            window.Height = 100;
            window.Width = 200;

            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = window.Width;
            double windowHeight = window.Height;
            window.Left = (screenWidth / 2) - (windowWidth / 2);
            window.Top = (screenHeight / 2) - (windowHeight / 2);


            window.ShowDialog();
        }
    }
}

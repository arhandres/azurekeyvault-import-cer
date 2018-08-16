using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
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
    }
}

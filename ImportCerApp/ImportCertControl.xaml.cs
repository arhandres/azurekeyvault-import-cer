using ImportCerApp.Model;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MMLib.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
using System.Windows.Threading;

namespace ImportCerApp
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ImportCertControl : UserControl
    {
        private X509Certificate2 _lastSelectedCertificate = null;
        private CertificateFile _lastSelectedFile = null;

        public ImportCertControl()
        {
            InitializeComponent();

            this.KeyVaultUrlText.Text = Config.GetValue<string>("DefaultKeyVaultUrl");
            this.ClientIdText.Text = Config.GetValue<string>("DefaultAdClientId");
            this.SecretText.Text = Config.GetValue<string>("DefaultAdClientSecret");
        }

        private CertificateFile OpenFileDialog()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.DefaultExt = ".pem";
            dialog.Filter = "PEM Files (*.pem)|*.pem|PFX Files (*.pfx)|*.pfx|Pkcs12 Files (*.p12)|*.p12";
            dialog.CheckPathExists = true;
            dialog.CheckFileExists = true;

            var result = dialog.ShowDialog();

            if (result ?? false)
            {
                return new CertificateFile()
                {
                    Path = dialog.FileName,
                    FileName = System.IO.Path.GetFileName(dialog.FileName),
                    Extention = System.IO.Path.GetExtension(dialog.FileName),
                    Content = File.ReadAllBytes(dialog.FileName)
                };
            }

            return null;
        }

        private void GetCertificateFile()
        {
            this.CertificateInfoGroup.Visibility = Visibility.Collapsed;

            var password = this.CertPasswordText.Password;
            var certFile = this.OpenFileDialog();

            if (certFile != null)
            {
                try
                {
                    var certificate = string.IsNullOrEmpty(password) ? new X509Certificate2(certFile.Content) : new X509Certificate2(certFile.Content, password);
                    this.PrintCertificateInfo(certificate);

                    _lastSelectedCertificate = certificate;
                    _lastSelectedFile = certFile;
                }
                catch (CryptographicException e)
                {
                    MessageBox.Show(e.Message, $"Can't open: {certFile.FileName}");
                }
            }
        }

        private void PrintCertificateInfo(X509Certificate2 certificate)
        {
            var dname = new X500DistinguishedName(certificate.SubjectName);
            var hasPrivate = certificate.HasPrivateKey;

            var sb = new StringBuilder();
            sb.AppendFormat("Distinguished Name: {0}{1}", dname.Name, Environment.NewLine);
            sb.AppendFormat("Expiration: {0}{1}", certificate.GetExpirationDateString(), Environment.NewLine);
            sb.AppendFormat("Has Pirvate Key: {0}{1}", certificate.HasPrivateKey ? "YES" : "NO", Environment.NewLine);

            this.CertificateInfoText.Text = sb.ToString();

            this.CertificateInfoGroup.Visibility = Visibility.Visible;
        }

        private KeyVaultClient CreateKeyValueClient()
        {
            var clientId = this.ClientIdText.Text;
            var clientSecret = this.SecretText.Text;

            var keyClient = new KeyVaultClient(async (authority, resource, scope) =>
            {
                var adCredential = new ClientCredential(clientId, clientSecret);
                var authenticationContext = new AuthenticationContext(authority, null);
                return (await authenticationContext.AcquireTokenAsync(resource, adCredential).ConfigureAwait(false)).AccessToken;
            });

            return keyClient;
        }

        private async Task ImportCertificate()
        {
            var client = this.CreateKeyValueClient();

            var url = this.KeyVaultUrlText.Text;
            var name = string.IsNullOrEmpty(_lastSelectedCertificate.FriendlyName) ? System.IO.Path.GetFileNameWithoutExtension(_lastSelectedFile.FileName) : _lastSelectedCertificate.FriendlyName;
            name = name.RemoveDiacritics().RemoveChars(" ,|_.");

            var password = this.CertPasswordText.Password;

            var base64EncodedCertificate = Convert.ToBase64String(_lastSelectedFile.Content);

            var result = await client.ImportCertificateAsync(url, name, base64EncodedCertificate, password);

            if (!string.IsNullOrEmpty(result.Id))
            {
                MessageBox.Show("Cert Imported!");
                Clear();
            }
        }

        private void Clear()
        {
            this.CertPasswordText.Clear();
            this.CertificateInfoText.Clear();

            this.CertificateInfoGroup.Visibility = Visibility.Collapsed;
        }

        private void CertSelectorButton_Click(object sender, RoutedEventArgs e)
        {
            this.GetCertificateFile();
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Window.GetWindow(this);

            mainWindow.Indicator.IsBusy = true;

            var error = string.Empty;

            await ImportCertificate().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    error = t.Exception?.Message;

            }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(error))
                MessageBox.Show(error);

            Dispatcher.Invoke(new Action(() => {
                mainWindow.Indicator.IsBusy = false;
            }), DispatcherPriority.ContextIdle);
        }
    }
}

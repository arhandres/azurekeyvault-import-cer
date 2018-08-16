using ImportCerApp.Model;
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

namespace ImportCerApp
{   
    public partial class CreateCertControl : UserControl
    {
        public CreateCertControl()
        {
            InitializeComponent();

            this.ValidFromDate.SelectedDate = DateTime.Now;
            this.ValidToDate.SelectedDate = DateTime.Now.AddYears(1);
        }

        private CertificateInfo GetCertificateInfo()
        {
            var name = this.NameTextBox.Text;
            var size = (string)((ComboBoxItem)this.KeySizeSelect.SelectedItem).Content;
            var hash = (string)((ComboBoxItem)this.HashSelect.SelectedItem).Content;
            var from = this.ValidFromDate.SelectedDate.GetValueOrDefault();
            var to = this.ValidToDate.SelectedDate.GetValueOrDefault();

            var flags = new List<X509KeyUsageFlags>();

            if (this.DigitalSignatureCheck.IsChecked ?? false)
                flags.Add(X509KeyUsageFlags.DigitalSignature);

            if (this.KeyEnciphermentCheck.IsChecked ?? false)
                flags.Add(X509KeyUsageFlags.KeyEncipherment);

            if (this.NonRepudiationCheck.IsChecked ?? false)
                flags.Add(X509KeyUsageFlags.NonRepudiation);

            if (this.KeyAgreementCheck.IsChecked ?? false)
                flags.Add(X509KeyUsageFlags.KeyAgreement);

            var parsedHash = HashAlgorithmName.SHA1;

            if (hash == "SHA256")
                parsedHash = HashAlgorithmName.SHA256;

            if (hash == "SHA512")
                parsedHash = HashAlgorithmName.SHA512;

            return new CertificateInfo()
            {
                Name = name,
                Size = int.Parse(size),
                Hash = parsedHash,
                From = from,
                To = to,
                IsCA = this.AsCACheck.IsChecked ?? false,
                UsaeFlags = flags,
                IsCritical = this.AllCriticalCheckbox.IsChecked ?? false
            };
        }

        private void CreateCertificate(OutputType output)
        {
            var info = this.GetCertificateInfo();

            var rsa = this.CreateRSAKey(info.Size);

            var request = new CertificateRequest(info.Name, rsa, info.Hash, RSASignaturePadding.Pss);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(info.IsCA, false, 0, info.IsCA));

            if (info.UsaeFlags != null && info.UsaeFlags.Any())
            {
                X509KeyUsageFlags flags = info.UsaeFlags.FirstOrDefault();
                foreach (var f in info.UsaeFlags.Skip(1))
                    flags |= f;

                request.CertificateExtensions.Add(new X509KeyUsageExtension(flags, info.IsCritical));
            }

            var outputDir = Config.GetValue<string>("OutputDir");
            var fullPath = string.Empty;

            switch (output)
            {
                case OutputType.CsrDer:
                    {
                        var csrBytes = request.CreateSigningRequest();

                        var fileName = $"DER_{DateTime.Now.ToString("yyyyMMddHHmmss")}.csr";
                        fullPath = System.IO.Path.Combine(outputDir, fileName);

                        File.WriteAllBytes(fullPath, csrBytes);//DER
                    }
                    break;
                case OutputType.CsrPem:
                    {
                        var csrBytes = request.CreateSigningRequest();
                        var csrBytesBase64 = Convert.ToBase64String(csrBytes);

                        var begin = "-----BEGIN CERTIFICATE REQUEST-----" + Environment.NewLine;
                        var end = "-----END CERTIFICATE REQUEST-----" + Environment.NewLine;

                        var content = $"{begin}{csrBytesBase64}{end}";

                        var fileName = $"PEM_{DateTime.Now.ToString("yyyyMMddHHmmss")}.csr";
                        fullPath = System.IO.Path.Combine(outputDir, fileName);

                        File.WriteAllText(fullPath, content);//PEM
                    }
                    break;
                case OutputType.CerDer:
                    {
                        var passwordDialog = new PasswordWindow();
                        var exportPassword = string.Empty;

                        var result = passwordDialog.ShowDialog();
                        if (result ?? false)
                            exportPassword = string.Equals(passwordDialog.Password, passwordDialog.PasswordConfirmation) ? passwordDialog.Password : string.Empty;

                        if (!string.IsNullOrEmpty(exportPassword))
                        {
                            var cer = request.CreateSelfSigned(info.From.ToUniversalTime(), info.To.ToUniversalTime());
                            var cerBytes = cer.Export(X509ContentType.Pfx, exportPassword);

                            var fileName = $"DER_{DateTime.Now.ToString("yyyyMMddHHmmss")}.pfx";
                            fullPath = System.IO.Path.Combine(outputDir, fileName);

                            File.WriteAllBytes(fullPath, cerBytes);//DER
                        }
                        else
                        {
                            MessageBox.Show("The password are different");
                            return;
                        }
                    }
                    break;
                case OutputType.CerPem:
                    {
                        //Not Implemented Yet
                    }
                    break;

            }

            MessageBox.Show($"Created: {fullPath}");
        }

        private RSA CreateRSAKey(int size)
        {
            return RSA.Create(size);
        }

        private void ViewAdvancedCheck_Checked(object sender, RoutedEventArgs e)
        {
            this.AdvancedGroup.Visibility = Visibility.Visible;
        }

        private void ViewAdvancedCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            this.AdvancedGroup.Visibility = Visibility.Collapsed;
        }

        private void CreateCsrButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = (string)((Button)sender).Tag;

            if (tag == "PEM")
                this.CreateCertificate(OutputType.CsrPem);

            if (tag == "DER")
                this.CreateCertificate(OutputType.CsrDer);
        }

        private void CreateCertButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = (string)((Button)sender).Tag;

            if (tag == "PEM")
                this.CreateCertificate(OutputType.CerPem);

            if (tag == "DER")
                this.CreateCertificate(OutputType.CerDer);
        }
    }

    public enum OutputType
    {
        CsrPem,
        CsrDer,
        CerPem,
        CerDer
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ImportCerApp.Model
{
    public class CertificateInfo
    {
        public string Name { get; set; }

        public int Size { get; set; }

        public HashAlgorithmName Hash { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public bool IsCA { get; set; }

        public List<X509KeyUsageFlags> UsaeFlags { get; set; }

        public bool IsCritical { get; set; }
    }
}

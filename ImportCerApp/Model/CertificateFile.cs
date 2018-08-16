using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportCerApp.Model
{
    class CertificateFile
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public string Extention { get; set; }
        public byte[] Content { get; set; }
    }
}

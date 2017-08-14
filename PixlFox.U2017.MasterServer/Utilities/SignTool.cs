using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PixlFox.U2017.Tools
{
    public class SignTool
    {
        public bool HasCertificate { get; private set; }
        private X509Certificate2 Certificate { get; set; }

        public SignTool() { }
        public SignTool(X509Certificate2 certificate)
        {
            Certificate = certificate;
        }

        public bool VerifyFileSignatures(params string[] files)
        {
            foreach (var file in files)
            {
                if (!File.Exists(file) || !File.Exists(file + ".sig"))
                    return false;

                byte[] data = File.ReadAllBytes(file);
                byte[] signature = Convert.FromBase64String(File.ReadAllText(file + ".sig"));
                if (!Certificate.GetRSAPublicKey().VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                    return false;
            }

            return true;
        }
    }
}

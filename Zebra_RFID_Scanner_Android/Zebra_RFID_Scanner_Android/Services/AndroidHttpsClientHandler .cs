using Java.Security;
using Javax.Net.Ssl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Android.Net;

namespace Zebra_RFID_Scanner_Android.Services
{
    public class AndroidHttpsClientHandler : AndroidClientHandler
    {
        private SSLContext sslContext;
        private const string clientCertPassword = "tO8RQjNU";

        public AndroidHttpsClientHandler(byte[] keystoreRaw) : base()
        {
            IKeyManager[] keyManagers = null;
            ITrustManager[] trustManagers = null;

            if (keystoreRaw != null)
            {
                using (MemoryStream memoryStream = new MemoryStream(keystoreRaw))
                {
                    KeyStore keyStore = KeyStore.GetInstance("pkcs12");
                    keyStore.Load(memoryStream, clientCertPassword.ToCharArray());
                    KeyManagerFactory kmf = KeyManagerFactory.GetInstance("x509");
                    kmf.Init(keyStore, clientCertPassword.ToCharArray());
                    keyManagers = kmf.GetKeyManagers();
                }
            }

            sslContext = SSLContext.GetInstance("TLS");
            sslContext.Init(keyManagers, trustManagers, null);
        }

        protected override SSLSocketFactory ConfigureCustomSSLSocketFactory(HttpsURLConnection
        connection)
        {
            SSLSocketFactory socketFactory = sslContext.SocketFactory;
            if (connection != null)
            {
                connection.SSLSocketFactory = socketFactory;
            }
            return socketFactory;
        }
    }
}

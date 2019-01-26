using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Credentials
    {
        public string NLUApiKey;
        public string NLUUrl;
        public string DBServer;
        public string DBLogin;
        public string DBPassword;
        public string DBConnection;
        public string Dbport;
        public string STTApiKey;
        public string GCloudApiKey;

        public Credentials()
        {

            var filePath = "credentials.csv";
            string[][] data = File.ReadLines(filePath).Where(line => line != "").Select(x => x.Split(';')).ToArray();
            NLUApiKey = data[1][0];
            NLUUrl = data[1][1];
            DBServer = data[1][2];
            DBLogin = data[1][3];
            DBPassword = data[1][4];
            DBConnection = data[1][5];
            Dbport = data[1][6];
            STTApiKey = data[1][7];
            GCloudApiKey = data[1][8];
        }


    }
}

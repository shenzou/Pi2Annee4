using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WhatsAppApi;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace Test
{
    class PeopleContact : Hub
    {
        string idBoitier;
        string nomTuteur;
        string prenomTuteur;
        string numeroTel;
        string numWhatsapp;
        string email;
        string facebookId;
        string adressePatient;
        string waNum;
        string waPass;
        static WhatsApp wa;

        public PeopleContact()
        {
            var filePath = "IDBoitier.csv";
            string[][] data = File.ReadLines(filePath).Where(line => line != "").Select(x => x.Split(';')).ToArray();
            idBoitier = data[0][0];
            /* A décommenter quand le site de login sera en place
            while(!TryToLogTheBox())
            {
                Console.WriteLine("Login problem. Trying again");

            }
            */
            if(waNum!=null && waPass!=null)
            {
                LoginWhatsapp(waNum, waPass);
            }
            
        }

        /* A décommenter quand le site de login sera en place
        public bool TryToLogTheBox(string idBoitier)
        {
            //Acces à l'adresse du site
            //Si connexion et réponse succès, return true
            //récupération des informations user et stockage dans les variables définies:
            nomTuteur=
            prenomTuteur=
            numeroTel=
            email=
            facebookId=
            adressePatient=
            //Sinon, return false
        }
        */

        public void SendMessage(string message)
        {
            if (waNum != null && waPass != null)
            {
                SendWhatsapp(numWhatsapp, message);
            }
        }


        #region Whatsapp

        public void SendWhatsapp(string to, string message)
        {
            if (wa.ConnectionStatus == ApiBase.CONNECTION_STATUS.LOGGEDIN)
            {
                wa.SendMessage(to, message);
                Clients.All.notifyMessage(string.Format("{0}: {1}", to, message));
            }
        }

        public void LoginWhatsapp(string phoneNumber, string password)
        {
            Thread thhread = new Thread(t =>
            {
                wa = new WhatsApp(phoneNumber, password, phoneNumber, true);
                wa.OnConnectSuccess += () =>
                {
                    Clients.All.notifyMessage("Connected......");
                    wa.OnLoginSuccess += (phone, data) =>
                    {
                        Clients.All.notifyMessage("Login Success !");
                    };
                    wa.OnLoginFailed += (data) =>
                    {
                        Clients.All.notifyMessage(string.Format("Login failed: {0}", data));
                    };
                    wa.Login();
                };
                wa.OnConnectFailed += (ex) =>
                {
                    Clients.All.notifyMessage(string.Format("Connected failed: {0}", ex.StackTrace));
                };
                wa.Connect();
            })
            { IsBackground = true };
            thhread.Start();
        }
        #endregion
    }
}

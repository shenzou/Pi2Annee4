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
using PushSharp;
using PushSharp.Google;
using PushSharp.Apple;
using PushSharp.Core;

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
        string GCloudKey;

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
            /*
            if(waNum!=null && waPass!=null)
            {
                LoginWhatsapp(waNum, waPass);
            }
            */
            Credentials creds = new Credentials();
            GCloudKey = creds.GCloudApiKey;
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
            /*
            if (waNum != null && waPass != null)
            {
                SendWhatsapp(numWhatsapp, message);
            }
            */

            // Configuration GCM (use this section for GCM)
            //var config = new GcmConfiguration("GCM-SENDER-ID", "AUTH-TOKEN", null);
            //var provider = "GCM";

            // Configuration FCM (use this section for FCM)
            var config = new GcmConfiguration(GCloudKey);
            config.GcmUrl = "https://fcm.googleapis.com/fcm/send";
            var provider = "FCM";

            // Create a new broker
            var gcmBroker = new GcmServiceBroker(config);

            // Wire up events
            gcmBroker.OnNotificationFailed += (notification, aggregateEx) => {

                aggregateEx.Handle(ex => {

                    // See what kind of exception it was to further diagnose
                    if (ex is GcmNotificationException notificationException)
                    {

                        // Deal with the failed notification
                        var gcmNotification = notificationException.Notification;
                        var description = notificationException.Description;

                        Console.WriteLine($"{provider} Notification Failed: ID={gcmNotification.MessageId}, Desc={description}");
                    }
                    else if (ex is GcmMulticastResultException multicastException)
                    {

                        foreach (var succeededNotification in multicastException.Succeeded)
                        {
                            Console.WriteLine($"{provider} Notification Succeeded: ID={succeededNotification.MessageId}");
                        }

                        foreach (var failedKvp in multicastException.Failed)
                        {
                            var n = failedKvp.Key;
                            var e = failedKvp.Value;

                            Console.WriteLine($"{provider} Notification Failed: ID={n.MessageId}, Desc={e.Description}");
                        }

                    }
                    else if (ex is DeviceSubscriptionExpiredException expiredException)
                    {

                        var oldId = expiredException.OldSubscriptionId;
                        var newId = expiredException.NewSubscriptionId;

                        Console.WriteLine($"Device RegistrationId Expired: {oldId}");

                        if (!string.IsNullOrWhiteSpace(newId))
                        {
                            // If this value isn't null, our subscription changed and we should update our database
                            Console.WriteLine($"Device RegistrationId Changed To: {newId}");
                        }
                    }
                    else if (ex is RetryAfterException retryException)
                    {

                        // If you get rate limited, you should stop sending messages until after the RetryAfterUtc date
                        Console.WriteLine($"{provider} Rate Limited, don't send more until after {retryException.RetryAfterUtc}");
                    }
                    else
                    {
                        Console.WriteLine("{provider} Notification Failed for some unknown reason");
                    }

                    // Mark it as handled
                    return true;
                });
            };

            gcmBroker.OnNotificationSucceeded += (notification) => {
                Console.WriteLine("{provider} Notification Sent!");
            };

            // Start the broker
            gcmBroker.Start();

            foreach (var regId in MY_REGISTRATION_IDS)
            {
                // Queue a notification to send
                gcmBroker.QueueNotification(new GcmNotification
                {
                    RegistrationIds = new List<string> {
            regId
        },
                    Data = JObject.Parse("{ \"somekey\" : \"somevalue\" }")
                });
            }

            // Stop the broker, wait for it to finish   
            // This isn't done after every message, but after you're
            // done with the broker
            gcmBroker.Stop();
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

        #region PushSharp


        #endregion
    }
}

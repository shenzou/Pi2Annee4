using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBM.WatsonDeveloperCloud.Util;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Net.WebSockets;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.IO;
using System.Runtime.Serialization;
using NAudio.Wave;

namespace Test
{
    class STT
    {
        private int userID;


        public STT(int userID)
        {
            this.userID = userID;
        }

        public void SpeechToText()
        {
            Credentials cred = new Credentials();
            IamTokenData token = GetIAMToken(cred.STTApiKey);
            WebSocketTest(token, userID).Wait();

        }


        ArraySegment<byte> openingMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            "{\"action\": \"start\", \"content-type\": \"audio/wav\", \"max_alternatives\": 0}"
        ));
        ArraySegment<byte> closingMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            "{\"action\": \"stop\"}"
        ));

        string audioFile = "record.wav";

        IamTokenData GetIAMToken(string apikey)
        {
            var wr = (HttpWebRequest)WebRequest.Create("https://iam.bluemix.net/identity/token");
            wr.Proxy = null;
            wr.Method = "POST";
            wr.Accept = "application/json";
            wr.ContentType = "application/x-www-form-urlencoded";

            using (TextWriter tw = new StreamWriter(wr.GetRequestStream()))
            {
                tw.Write($"grant_type=urn:ibm:params:oauth:grant-type:apikey&apikey={apikey}");
            }
            var resp = wr.GetResponse();
            using (TextReader tr = new StreamReader(resp.GetResponseStream()))
            {
                var s = tr.ReadToEnd();
                return JsonConvert.DeserializeObject<IamTokenData>(s);
            }
        }

        async Task WebSocketTest(IamTokenData token, int userID)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            ClientWebSocket clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.Proxy = null;
            clientWebSocket.Options.SetRequestHeader("Authorization", $"Bearer {token.AccessToken}");

            Uri connection = new Uri($"wss://gateway-syd.watsonplatform.net/speech-to-text/api/v1/recognize?model=fr-FR_BroadbandModel");
            try
            {
                await clientWebSocket.ConnectAsync(connection, cts.Token);
                Console.WriteLine("Connected!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to connect: " + e.ToString());
                return;
            }

            // send opening message and wait for initial delimeter 
            Task.WaitAll(clientWebSocket.SendAsync(openingMessage, WebSocketMessageType.Text, true, CancellationToken.None), HandleResults(clientWebSocket, userID));

            // send all audio and then a closing message; simltaneously print all results until delimeter is recieved
            Task.WaitAll(SendAudio(clientWebSocket), HandleResults(clientWebSocket, userID));

            // close down the websocket
            clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None).Wait();


        }

        async Task SendAudio(ClientWebSocket ws)
        {

            using (FileStream fs = File.OpenRead(audioFile))
            {
                byte[] b = new byte[1024];
                while (fs.Read(b, 0, b.Length) > 0)
                {
                    await ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
                await ws.SendAsync(closingMessage, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        // prints results until the connection closes or a delimeterMessage is recieved
        async Task HandleResults(ClientWebSocket ws, int userID)
        {
            var buffer = new byte[1024];
            while (true)
            {
                var segment = new ArraySegment<byte>(buffer);

                var result = await ws.ReceiveAsync(segment, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                int count = result.Count;
                while (!result.EndOfMessage)
                {
                    if (count >= buffer.Length)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "That's too long", CancellationToken.None);
                        return;
                    }

                    segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                    result = await ws.ReceiveAsync(segment, CancellationToken.None);
                    count += result.Count;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, count);

                // you'll probably want to parse the JSON into a useful object here,
                // see ServiceState and IsDelimeter for a light-weight example of that.
                Console.WriteLine(message);


                string listeningMessage = "{\n   \"state\": \"listening\"\n}";
                if (message != listeningMessage)
                {
                    getText(message, userID);
                    DBManagement db = new DBManagement(userID);
                    db.AddRequestHistory(message);
                }

                if (IsDelimeter(message))
                {
                    return;
                }
            }
        }

        async void getText(String message, int userID)
        {
            int Start, End;
            string strStart = "transcript\": \"";
            string strEnd = "\"\n            }\n         ], \n         \"fin";
            string confidence = "0";
            if (message.Contains(strStart) && message.Contains(strEnd))
            {
                Start = message.IndexOf(strStart, 0) + strStart.Length;
                End = message.IndexOf(strEnd, Start);
                //Console.WriteLine(message.Substring(Start, End - Start));
                string answer = message.Substring(Start, End - Start);
                confidence = getConfidence(message);
                DBManagement dB = new DBManagement(userID);
                dB.AddVoiceTranscript(answer, confidence);
            }
            else
            {
                string answer = "Incomplete.";
                DBManagement dB = new DBManagement(userID);
                dB.AddVoiceTranscript(answer, confidence);
            }


        }

        string getConfidence(String message) //Indice de confiance pour la requete Speech to text
        {
            int Start, End;
            string strStart = "confidence\": ";
            string strEnd = ", \n               \"transcript";
            string confidence = "0";
            if (message.Contains(strStart) && message.Contains(strEnd))
            {
                Start = message.IndexOf(strStart, 0) + strStart.Length;
                End = message.IndexOf(strEnd, Start);
                confidence = message.Substring(Start, End - Start);
            }
            return confidence;
        }

        [DataContract]
        internal class ServiceState
        {
            [DataMember]
            public string state = "";
        }
        bool IsDelimeter(String json)
        {
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ServiceState));
            ServiceState obj = (ServiceState)ser.ReadObject(stream);
            return obj.state == "listening";
        }
    }
}

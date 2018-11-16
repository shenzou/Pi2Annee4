using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1.Model;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
using IBM.WatsonDeveloperCloud.Util;
using System.Net.Http;
using System.Net.Http.Formatting;
using IBM.WatsonDeveloperCloud.Http.Filters;
using IBM.WatsonDeveloperCloud.Http;
using IBM.WatsonDeveloperCloud.Service;
using Newtonsoft.Json;
using IBM.WatsonDeveloperCloud.SpeechToText.v1;
using System.Runtime.InteropServices;
using System.Net.WebSockets;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.IO;
using System.Runtime.Serialization;




namespace Test
{
    class Program
    {
        
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)] //Windows Multimedia API
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        //Packages à installer via NuGet
        //Install-Package IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1
        //Install-Package IBM.WatsonDeveloperCloud.SpeechToText.v1
        //Install-Package IBM.WatsonDeveloperCloud.ToneAnalyzer.v3
        

        static void Main(string[] args)
        {
            
            NLU nluElement = SetupNLU();
            //testsApiNLU(nluElement);
            SpeechToText();
            //RecordAndPlayAudio();
            //DBManagement DB = new DBManagement();
            //DB.AddUserLog("first test");

            Console.ReadKey();
        }

        
        #region Speech To Text

        static void SpeechToText()
        {
            Credentials cred = new Credentials();
            IamTokenData token = GetIAMToken(cred.STTApiKey);
            WebSocketTest(token);
        }


        static ArraySegment<byte> openingMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            "{\"action\": \"start\", \"content-type\": \"audio/wav\", \"continuous\" : true, \"interim_results\": true}"
        ));
        static ArraySegment<byte> closingMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
            "{\"action\": \"stop\"}"
        ));

        static string audioFile = "record.wav";

        static IamTokenData GetIAMToken(string apikey)
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

        static async Task WebSocketTest(IamTokenData token)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            ClientWebSocket clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.Proxy = null;
            clientWebSocket.Options.SetRequestHeader("Authorization", $"Bearer {token.AccessToken}");
            Uri connection = new Uri($"wss://gateway-syd.watsonplatform.net/speech-to-text/api/v1/recognize");
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
            Task.WaitAll(clientWebSocket.SendAsync(openingMessage, WebSocketMessageType.Text, true, CancellationToken.None), HandleResults(clientWebSocket));

            // send all audio and then a closing message; simltaneously print all results until delimeter is recieved
            Task.WaitAll(SendAudio(clientWebSocket), HandleResults(clientWebSocket));

            // close down the websocket
            clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None).Wait();

            
        }

        static async Task SendAudio(ClientWebSocket ws)
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
        static async Task HandleResults(ClientWebSocket ws)
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

                

                /*
                DBManagement db = new DBManagement();
                db.AddRequestHistory(message);
                */

                if (IsDelimeter(message))
                {
                    return;
                }
            }
        }

        /*
        static void STTAnswer(String json)
        {
            try
            {
                STTJson answer = JsonConvert.DeserializeObject<STTJson>(json);
                Console.WriteLine(answer.results);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        */

        [DataContract]
        internal class ServiceState
        {
            [DataMember]
            public string state = "";
        }
        static bool IsDelimeter(String json)
        {
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ServiceState));
            ServiceState obj = (ServiceState)ser.ReadObject(stream);
            return obj.state == "listening";
        }
        


        /*
        static  void SpeechToText() //A terminer
        {
            SpeechToTextService STTService = new SpeechToTextService();
            STTService = SetupSTT(STTService);
            Console.WriteLine("Language of the file:");
            Console.WriteLine("1. fr-FR_BroadbandModel");
            Console.WriteLine("2. en-US_BroadbandModel");
            Console.WriteLine("3. ja-JP_BroadbandModel");
            int caseSwitch = 0;
            while(caseSwitch==0)
            {
                try
                {
                    caseSwitch = Convert.ToInt32(Console.ReadLine());
                }
                catch(Exception e)
                {

                }
            }
            switch(caseSwitch)
            {
                case 1: var session = STTService.CreateSession("en-US_BroadbandModel");
                    break;

            }
            
        }

        static SpeechToTextService SetupSTT(SpeechToTextService STTService) //A terminer
        {
            TokenOptions connectorSTT = new TokenOptions();
            connectorSTT.IamApiKey = "gm7GT16FTkQ2-yp1vfHSbeqpFPS0xv3uI_w02HoWstOQ";
            connectorSTT.ServiceUrl = "https://gateway-syd.watsonplatform.net/speech-to-text/api";
            STTService = new SpeechToTextService();
            STTService.SetCredential(connectorSTT);
            return STTService;
        }
        */
        #endregion

        #region Natural Language Understanding
        static NLU SetupNLU() //Demo NLU
        {
            Credentials cred = new Credentials();
            NaturalLanguageUnderstandingService NLUService = new NaturalLanguageUnderstandingService();
            //NLUService = SetupNLU(NLUService);
            string IamApiKey = cred.NLUApiKey;
            string ServiceUrl = cred.NLUUrl;
            NLU nluElement = new NLU(IamApiKey, ServiceUrl, NLUService);
            return nluElement;
        }

        static void testsApiNLU(NLU nlu)
        {
            string text = "Je ne souhaite pas être aidé, laissez-moi tranquille s'il vous plait ";
            string URL = "https://www.20minutes.fr/politique/2369135-20181110-video-armee-europeenne-emmanuel-macron-tente-apaiser-tensions-donald-trump";
            var request = nlu.URLInfo(URL);
            //var request = nlu.TextInfo(text);
            Console.WriteLine(JsonConvert.SerializeObject(request, Formatting.Indented));
        }

        #endregion

        #region AudioFile
        static void RecordAndPlayAudio()
        {
            Console.WriteLine("Program micro   presser une touche pour commancer l'enregistrement ");
            Console.ReadKey();

            Console.WriteLine("presser une touche pour arreter l'enregistrement");
            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
            mciSendString("record recsound", "", 0, 0);
            Console.ReadKey();

            mciSendString("save recsound record.wav", "", 0, 0); //Fichier sauvegardé dans bin/debug
            mciSendString("close recsound ", "", 0, 0);
            Console.WriteLine("Sauvergardee ");
            Console.ReadKey();


            string FileName = "record.wav";
            string CommandString = "open " + "\"" + FileName + "\"" + " type waveaudio alias recsound";
            mciSendString(CommandString, null, 0, 0);
            CommandString = "play recsound";
            mciSendString(CommandString, null, 0, 0);


            Console.ReadKey();

            Console.ReadKey();

        }

        #endregion
    }
}

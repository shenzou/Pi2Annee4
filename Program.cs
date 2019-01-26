using System;
using System.Text;
using System.Threading.Tasks;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
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
    class Program
    {
        
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)] //Windows Multimedia API
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        //Packages à installer via NuGet
        //Install-Package IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1
        //Install-Package IBM.WatsonDeveloperCloud.SpeechToText.v1
        //Install-Package IBM.WatsonDeveloperCloud.ToneAnalyzer.v3
        //Install-Package NAudio -Version 1.8.5
        //Install-Package Whatsapp.NET -Version 1.2.2
        //Install-Package Microsoft.AspNet.SignalR -Version 2.4.0
        //Install-Package PushSharp -Version 4.0.10


        static void Main(string[] args)
        {
            
            NLU nluElement = SetupNLU(); //Création d'un élément Natural Language Understanding
            int userID = 211594;
            STT sttElement = new STT(userID); //Génération d'un élément de classe speech to text en fonction de l'ID utiisateur.

            //RecordAndPlayAudio(); //Enregistrement d'un fichier audio du PC, A REGLER POUR RASPBERRY
            //testsApiNLU(nluElement); //Pour démontrer le fonctionnement de NLU


            sttElement.SpeechToText(); //Envoi d'une requête speech to text
            
            DBManagement DB = new DBManagement(userID); //Gestion de la database
            //DB.AddUserLog("first test");
            Console.WriteLine("Dernière transcription: " + DB.LastVoiceTranscript()); //Affichage de la transcription Speech to text depuis la database
            ApiNLU(nluElement, DB.LastVoiceTranscript()); //Exécute la requête natural language understanding en fonction du dernier élément enregistré dans la DB.
            Console.ReadKey();
        }



        
        #region Speech To Text

        
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

        static void ApiNLU(NLU nlu, string text)
        {
            //var request = nlu.URLInfo(URL);
            var request = nlu.TextInfo(text);
            Console.WriteLine(JsonConvert.SerializeObject(request, Formatting.Indented));
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

            mciSendString("save recsound recordTemp.wav", "", 0, 0); //Fichier sauvegardé dans bin/debug
            mciSendString("close recsound ", "", 0, 0);
            Console.WriteLine("Sauvergardee ");
            Console.ReadKey();


            string FileName = "recordTemp.wav";
            string CommandString = "open " + "\"" + FileName + "\"" + " type waveaudio alias recsound";
            mciSendString(CommandString, null, 0, 0);
            CommandString = "play recsound";
            mciSendString(CommandString, null, 0, 0);

            WaveFileReader reader = new NAudio.Wave.WaveFileReader("recordTemp.wav");

            WaveFormat newFormat = new WaveFormat(16000, 16, 1);

            WaveFormatConversionStream str = new WaveFormatConversionStream(newFormat, reader);

            try
            {
                WaveFileWriter.CreateWaveFile("record.wav", str);
                Console.WriteLine("Audio converted to 16Khz");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                str.Close();
            }

            
        


        Console.ReadKey();


        }




        #endregion
    }
}

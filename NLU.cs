using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1.Model;
using IBM.WatsonDeveloperCloud.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class NLU
    {
        TokenOptions connectorNLU;
        string versionDate;
        NaturalLanguageUnderstandingService NLUService;

        public NLU(string apiKey, string serviceURL, NaturalLanguageUnderstandingService NLUService)
        {
            this.versionDate = "2017-02-27";
            this.connectorNLU = new TokenOptions();
            this.connectorNLU.IamApiKey = apiKey;
            this.connectorNLU.ServiceUrl = serviceURL;
            this.NLUService = NLUService;

            this.NLUService.SetCredential(connectorNLU);
            this.NLUService.VersionDate = versionDate;

        }

        public AnalysisResults TextInfo(string text) //Analyse de texte avec NLU
        {
            Parameters parameters = new Parameters()
            {
                Text = text,
                Language = "fr",
                Features = new Features()
                {
                    Sentiment = new SentimentOptions(),
                    Keywords = new KeywordsOptions()
                    {
                        Limit = 8,
                        Sentiment = true,
                        Emotion = true
                    }
                }
            };



            var result = NLUService.Analyze(parameters);
            return result;
            
        }

        public AnalysisResults URLInfo(string URL) //Analyse d'un site web avec NLU
        {

            Parameters parameters = new Parameters()
            {
                Url = URL,
                Features = new Features()
                {
                    Sentiment = new SentimentOptions(),
                    Keywords = new KeywordsOptions()
                    {
                        Limit = 8,
                        Sentiment = true,
                        Emotion = true
                    }
                }
            };


            var result = NLUService.Analyze(parameters);
            return result;
        }
    }
}

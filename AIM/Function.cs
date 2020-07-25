using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Alexa.NET.Request;
using Alexa.NET.Response;
using System.Net.Sockets;
using System.Text;
using Alexa.NET.Request.Type;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AIM
{
    public class Function
    {

        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            string responseString = "";
            
            if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;

                switch(intentRequest.Intent.Name)
                {
                    case "move":
                        var slots = intentRequest.Intent.Slots;
                        var itemName = slots["item"].Resolution.Authorities[0].Values[0].Value.Name;
                        var firstClass = slots["firstClass"].Resolution.Authorities[0].Values[0].Value.Name;
                        var secondClass = slots["secondClass"].Resolution.Authorities[0].Values[0].Value.Name;

                        Network.Send(string.Join('-', new string[] { itemName, firstClass, secondClass }));
                        //if (slots.Keys.Count)

                        break;
                    default:
                        break;
                }

                responseString = Network.Receive();
                Network.CloseClient();
            }
            
            return VoiceResponse(responseString);
        }
        
        private SkillResponse VoiceResponse(string text)
        {

            var speech = new Alexa.NET.Response.SsmlOutputSpeech
            {
                Ssml = string.Format("<speak>{0}</speak>", text)
            };

            var finalResponse = Alexa.NET.ResponseBuilder.Tell(speech);
            return finalResponse;

        }
    }
}

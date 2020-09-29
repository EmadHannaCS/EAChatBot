using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2;
using IBM.Watson.Assistant.v2.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using ViewModels;

namespace Helpers
{
    public class WatsonHelper
    {
        private const string ApiKey = "83lOPBncWMlhvCPSEmFsGKmI4bMZP0SV5Re4RKTw8SrH";
        private const string ApiUrl = "https://api.eu-de.assistant.watson.cloud.ibm.com/instances/176bf4ac-aab9-4c30-9d49-f87ba0ad4883";
        private const string AssistantId = "76d9c3e0-aa0e-4572-b16f-b9622ba6a240";


        private static IamAuthenticator authenticator = new IamAuthenticator(
      apikey: ApiKey
      );

        private static AssistantService assistant = new AssistantService("2020-09-29", authenticator);



        public static Output Consume(string Text = "", string Intent = "Range", string Entity = "Max", string Value = "")
        {
            assistant.SetServiceUrl(ApiUrl);
            assistant.DisableSslVerification(true);

            var session = assistant.CreateSession(assistantId: AssistantId);

            var response = assistant.Message(AssistantId, session.Result.SessionId, new MessageInput()
            {
                Intents = new List<RuntimeIntent> { new RuntimeIntent { Intent = Intent } },
                Entities = new List<RuntimeEntity> { new RuntimeEntity { Entity = Entity, Value = Value } }
            ,
                Text = Text
            });

            Output output = JsonSerializer.Deserialize<Output>(response.Response);

            return output;

        }
    }
}

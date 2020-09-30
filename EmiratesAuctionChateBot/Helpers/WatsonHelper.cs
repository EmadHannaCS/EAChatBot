using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2;
using IBM.Watson.Assistant.v2.Model;
using System.Collections.Generic;
using System.Text.Json;

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

        private static DetailedResponse<SessionResponse> session;


        public static MessageResponse Consume(string Text = "")
        {
            DetailedResponse<MessageResponse> messageResponse;
            assistant.SetServiceUrl(ApiUrl);
            assistant.DisableSslVerification(true);

            

            if (session == null)
            {
                session = assistant.CreateSession(AssistantId);
            }

            messageResponse = assistant.Message(AssistantId, session.Result.SessionId, new MessageInput() { Text = Text });

            MessageResponse watsonResponse = messageResponse.Result;


            return watsonResponse;

        }
    }
}

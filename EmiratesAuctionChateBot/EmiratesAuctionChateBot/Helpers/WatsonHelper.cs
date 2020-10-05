using BL.Managers;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2;
using IBM.Watson.Assistant.v2.Model;
using System.Collections.Generic;
using System.Text.Json;

namespace EmiratesAuctionChateBot.Helpers
{
    public class WatsonHelper : IWatsonHelper
    {
        private const string ApiKey = "83lOPBncWMlhvCPSEmFsGKmI4bMZP0SV5Re4RKTw8SrH";
        private const string ApiUrl = "https://api.eu-de.assistant.watson.cloud.ibm.com/instances/176bf4ac-aab9-4c30-9d49-f87ba0ad4883";
        private const string AssistantId = "a725d387-c5c7-4ced-bf76-65e249aa7412";
        private readonly ISessionsManager _sessionsManager;


        public WatsonHelper(ISessionsManager sessionsManager)
        {
            _sessionsManager = sessionsManager;

        }


        private static IamAuthenticator authenticator = new IamAuthenticator(
      apikey: ApiKey
      );

        private static AssistantService assistant = new AssistantService("2020-09-29", authenticator);

        private static Dictionary<string, DetailedResponse<SessionResponse>> UserSession = new Dictionary<string, DetailedResponse<SessionResponse>>();


        public MessageResponse Consume(string phone, string Text = "", bool isStart = false)
        {
            DetailedResponse<MessageResponse> messageResponse;
            assistant.SetServiceUrl(ApiUrl);
            assistant.DisableSslVerification(true);

            var sessionId = string.Empty;
            if (isStart)
            {
                UserSession[phone] = assistant.CreateSession(AssistantId);
                _sessionsManager.SetSession(phone, UserSession[phone].Result.SessionId);

            }
            else if (UserSession[phone] == null)
            {
                UserSession[phone] = assistant.CreateSession(AssistantId);

                sessionId = _sessionsManager.GetSession(phone)?.LastSessionId;
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    _sessionsManager.SetSession(phone, UserSession[phone].Result.SessionId);

                }
                else
                {
                    UserSession[phone].Result.SessionId = sessionId;
                }
            }

            messageResponse = assistant.Message(AssistantId, UserSession[phone].Result.SessionId, new MessageInput() { Text = Text });

            MessageResponse watsonResponse = messageResponse.Result;


            return watsonResponse;

        }
    }
}

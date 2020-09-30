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
        private const string AssistantId = "76d9c3e0-aa0e-4572-b16f-b9622ba6a240";
        private readonly ISessionsManager _sessionsManager;


        public WatsonHelper(ISessionsManager sessionsManager)
        {
            _sessionsManager = sessionsManager;

        }


        private static IamAuthenticator authenticator = new IamAuthenticator(
      apikey: ApiKey
      );

        private static AssistantService assistant = new AssistantService("2020-09-29", authenticator);

        private static DetailedResponse<SessionResponse> session;


        public MessageResponse Consume(string phone, string Text = "", bool isStart = false)
        {
            DetailedResponse<MessageResponse> messageResponse;
            assistant.SetServiceUrl(ApiUrl);
            assistant.DisableSslVerification(true);


            var sessionId = string.Empty;
            if (isStart)
            {
                session = assistant.CreateSession(AssistantId);
                _sessionsManager.SetSession(phone, session.Result.SessionId);

            }
            else if (session == null)
            {
                session = assistant.CreateSession(AssistantId);

                sessionId = _sessionsManager.GetSession(phone);
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    _sessionsManager.SetSession(phone, session.Result.SessionId);

                }
                else
                {
                    session.Result.SessionId = sessionId;
                }
            }

            messageResponse = assistant.Message(AssistantId, session.Result.SessionId, new MessageInput() { Text = Text });

            MessageResponse watsonResponse = messageResponse.Result;


            return watsonResponse;

        }
    }
}

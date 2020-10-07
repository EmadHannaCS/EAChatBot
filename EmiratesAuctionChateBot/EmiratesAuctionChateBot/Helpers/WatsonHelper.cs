using BL.Managers;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2;
using IBM.Watson.Assistant.v2.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EmiratesAuctionChateBot.Helpers
{
    public class WatsonHelper : IWatsonHelper
    {
        private readonly string ApiKey = string.Empty;
        private readonly string ApiUrl = string.Empty;
        AssistantService assistant;
        private readonly string AssistantId = string.Empty;
        private readonly ISessionsManager _sessionsManager;
        private readonly IConfiguration _config;


        public WatsonHelper(ISessionsManager sessionsManager, IConfiguration config)
        {
            _sessionsManager = sessionsManager;
            _config = config;
            ApiUrl = _config.GetSection("Watson").GetValue<string>("APIUrl");
            ApiKey = _config.GetSection("Watson").GetValue<string>("APIKey");
            AssistantId = _config.GetSection("Watson").GetValue<string>("AssistantId");
            IamAuthenticator authenticator = new IamAuthenticator(apikey: ApiKey);
            assistant = new AssistantService("2020-09-29", authenticator);
        }




        private Dictionary<string, DetailedResponse<SessionResponse>> UserSession = new Dictionary<string, DetailedResponse<SessionResponse>>();


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

        public string ToEnglishNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;


            string EnglishNumbers = "";

            for (int i = 0; i < input.Length; i++)
            {
                if (Char.IsDigit(input[i]))
                {
                    EnglishNumbers += char.GetNumericValue(input, i);
                }
                else
                {
                    EnglishNumbers += input[i].ToString();
                }
            }
            return EnglishNumbers;
        }
    }
}

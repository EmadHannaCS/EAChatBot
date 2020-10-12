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
        private readonly string ArabicAssistantId = string.Empty;
        private readonly ISessionsManager _sessionsManager;
        private readonly IConfiguration _config;


        public WatsonHelper(ISessionsManager sessionsManager, IConfiguration config)
        {
            _sessionsManager = sessionsManager;
            _config = config;
            ApiUrl = _config.GetSection("Watson").GetValue<string>("APIUrl");
            ApiKey = _config.GetSection("Watson").GetValue<string>("APIKey");
            AssistantId = _config.GetSection("Watson").GetValue<string>("AssistantId");
            ArabicAssistantId = _config.GetSection("Watson").GetValue<string>("ArabicAssitantId");
            IamAuthenticator authenticator = new IamAuthenticator(apikey: ApiKey);
            assistant = new AssistantService("2020-09-29", authenticator);
        }




        private static Dictionary<string, DetailedResponse<SessionResponse>> UserSession = new Dictionary<string, DetailedResponse<SessionResponse>>();


        public MessageResponse Consume(string phone, string Text = "", bool isStart = false, bool isNormalChat = false)
        {

            DetailedResponse<MessageResponse> messageResponse;
            assistant.SetServiceUrl(ApiUrl);
            assistant.DisableSslVerification(true);

            var sessionId = string.Empty;
            if (isStart)
            {
                UserSession[phone] = assistant.CreateSession(isNormalChat ? ArabicAssistantId : AssistantId);
                _sessionsManager.SetSession(phone, UserSession[phone].Result.SessionId);

            }
            else if (UserSession[phone] == null)
            {
                UserSession[phone] = assistant.CreateSession(isNormalChat ? ArabicAssistantId : AssistantId);

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

            messageResponse = assistant.Message(isNormalChat ? ArabicAssistantId : AssistantId, UserSession[phone].Result.SessionId, new MessageInput() { Text = Text });

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




        public List<KeyValuePair<int, string>> GetChoises(string message)
        {
            List<int> choisesNum = new List<int>();
            List<KeyValuePair<int, string>> choises = new List<KeyValuePair<int, string>>();
            for (int i = 0; i < message.Length; i++)
            {
                if (Char.IsDigit(message[i]) && (message[i + 1] == '-' || message[i + 2] == '-'))
                {
                    choisesNum.Add(int.Parse(message[i].ToString()));
                }
            }

            for (int i = 0; i < choisesNum.Count; i++)
            {
                int FirstIndex = message.IndexOf(choisesNum[i].ToString()) + 2;
                int LastIndex = i == choisesNum.Count - 1 ? 0 : message.IndexOf(choisesNum[i + 1].ToString());
                choises.Add(new KeyValuePair<int, string>(choisesNum[i], message.Substring(FirstIndex, LastIndex == 0 ? message.Length - 1 - FirstIndex : LastIndex - FirstIndex)));
            }

            return choises;
        }
    }
}

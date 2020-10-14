using BL.Managers;
using Helpers;
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

            DetailedResponse<MessageResponse> messageResponse = null;
            assistant.SetServiceUrl(ApiUrl);
            assistant.DisableSslVerification(true);

            var sessionId = string.Empty;
            if (!UserSession.ContainsKey(phone))
            {
                UserSession[phone] = null;
            }
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
            try
            {
                messageResponse = assistant.Message(isNormalChat ? ArabicAssistantId : AssistantId, UserSession[phone].Result.SessionId, new MessageInput() { Text = Text });

            }
            catch (Exception ex)
            {
                UserSession[phone] = assistant.CreateSession(isNormalChat ? ArabicAssistantId : AssistantId);
                _sessionsManager.SetSession(phone, UserSession[phone].Result.SessionId);
                messageResponse = assistant.Message(isNormalChat ? ArabicAssistantId : AssistantId, UserSession[phone].Result.SessionId, new MessageInput() { Text = Text });
                LogHelper.LogException(ex);
            }

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




        public Dictionary<int, string> GetChoises(string message)
        {
            List<int> choisesNum = new List<int>();
            int indexOfLast = 0;
            int lastIndex = 0;
            Dictionary<int, string> choises = new Dictionary<int, string>();
            int j = 0;
            for (int i = 0; i < message.Length; i++)
            {
                if (Char.IsDigit(message[i]) && message[i + 1] == '-')
                {
                    choisesNum.Add(int.Parse(message[i].ToString()));
                    int FirstIndex = i + 2;
                    int indexOfNextNumber = message.LastIndexOf((choisesNum[j] + 1).ToString());

                    if (indexOfNextNumber == -1 && message.LastIndexOf('0') > -1 && message.LastIndexOf('0') > indexOfNextNumber)
                    {
                        lastIndex = message.LastIndexOf('0');
                    }

                    if (choisesNum[j] + 1 == 1)
                    {
                        lastIndex = 0;
                        indexOfNextNumber = -1;
                    }
                    if (indexOfNextNumber > -1 && choisesNum[j] + 1 != 1)
                    {
                        lastIndex = message.LastIndexOf((choisesNum[j] + 1).ToString()) + 1;
                    }


                    if ((indexOfNextNumber > -1 && message[lastIndex] == '-') || j > 0)
                    {
                        indexOfLast = lastIndex == 0 ? message.LastIndexOf(message[message.Length - 1]) + 1 : lastIndex;
                        choises.Add(choisesNum[j], message.Substring(FirstIndex, indexOfNextNumber == -1 ? indexOfLast - FirstIndex : indexOfNextNumber - FirstIndex));
                    }
                    j++;
                    lastIndex = 0;
                    indexOfLast = 0;
                }
            }
            return choises;
        }
    }
}

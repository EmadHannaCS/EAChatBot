﻿using Helpers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using ViewModels;

namespace Helpers
{
    public class WebHookHelper : IWebHookHelper
    {
        private readonly IConfiguration _config;
        private readonly string apikey = string.Empty;
        private readonly string baseUrl = string.Empty;

        public WebHookHelper(IConfiguration config)
        {
            _config = config;
            apikey = _config.GetSection("WebHook").GetValue<string>("apikey");
            baseUrl = _config.GetSection("WebHook").GetValue<string>("baseUrl");


        }

        public IRestResponse sendTXTMsg(string phone, string msg)
        {

            if (string.IsNullOrWhiteSpace(msg) || string.IsNullOrWhiteSpace(phone))
                return null;


            var client = new RestClient(baseUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", apikey);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            WebHookRequestBody body = new WebHookRequestBody()
            {
                destinations = new List<destination>() {
                    new destination() {
                        to = new to() {
                            phoneNumber = phone
                        }
                    }
                },
                whatsApp = new whatsApp()
                {
                    text = msg
                }
            };
            string parameters = JsonConvert.SerializeObject(body);
            request.AddParameter("application/json", parameters, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return response;


        }
    }
}

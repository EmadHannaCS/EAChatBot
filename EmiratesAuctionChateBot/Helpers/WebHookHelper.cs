using Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

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

        public HttpResponseMessage sendTXTMsg(string phone, string msg)
        {

            if (string.IsNullOrWhiteSpace(msg) || string.IsNullOrWhiteSpace(phone))
                return null;

            string Url = "?apikey=" + System.Web.HttpUtility.UrlEncode(apikey) + "&number=" + System.Web.HttpUtility.UrlEncode(phone) + "&text=" + System.Web.HttpUtility.UrlEncode(msg);
            HttpResponseMessage message = WebClientHelper.Consume(baseUrl, HttpMethod.Get, Url);

            return message;


        }
    }
}

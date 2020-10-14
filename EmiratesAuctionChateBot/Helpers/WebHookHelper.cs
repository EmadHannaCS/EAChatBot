using Helpers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Helpers
{
    public class WebHookHelper
    {

        private const string apikey = "L7B9WYYN704C5LJLLRR7";
        private const string baseUrl = "https://panel.rapiwha.com/send_message.php";

        public static HttpResponseMessage sendTXTMsg(string phone, string msg)
        {
            if (string.IsNullOrWhiteSpace(msg) || string.IsNullOrWhiteSpace(phone))
                return null;

            string Url = "?apikey=" + System.Web.HttpUtility.UrlEncode(apikey) + "&number=" + System.Web.HttpUtility.UrlEncode(phone) + "&text=" + System.Web.HttpUtility.UrlEncode(msg);
            HttpResponseMessage message = WebClientHelper.Consume(baseUrl, HttpMethod.Get, Url);

            return message;


        }
    }
}

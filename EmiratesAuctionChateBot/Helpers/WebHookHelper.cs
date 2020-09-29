using Helpers.WebClent;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace ViewModels
{
    public class WebHookHelper
    {

        private const string apikey = "5B89KO6RF192SGMP37CK";
        private const string baseUrl = "https://panel.rapiwha.com/send_message.php";

        public static HttpResponseMessage sendTXTMsg(string phone, string msg)
        {

            string Url = "?apikey=" + System.Web.HttpUtility.UrlEncode(apikey) + "&number=" + System.Web.HttpUtility.UrlEncode(phone) + "&text=" + System.Web.HttpUtility.UrlEncode(msg);
            HttpResponseMessage message = WebClientHelper.Consume(baseUrl, HttpMethod.Get, Url);

            return message;


        }
    }
}

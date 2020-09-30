using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public class WebClientHelper
    {
        public static HttpResponseMessage Consume(string baseurl, HttpMethod method, string relativeUrl,
           string jsonObj = null, string lang = "", string basicAuthUser = "", string basicAuthPassword = "")
        {
            using (var client = new HttpClient())
            {
                //client.Timeout.Add(new TimeSpan(0, 3, 0));
                client.BaseAddress = new Uri(baseurl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(basicAuthUser) && !string.IsNullOrWhiteSpace(basicAuthPassword))
                {
                    var byteArray = new UTF8Encoding().GetBytes(basicAuthUser + ":" + basicAuthPassword);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                }
                //client.DefaultRequestHeaders.Add("Accept-Language", lang);
                HttpRequestMessage request = new HttpRequestMessage(method, relativeUrl);
                if (jsonObj != null)
                    request.Content = new StringContent(jsonObj, Encoding.UTF8, "application/json");
                var Res = new HttpResponseMessage();
                try
                {
                    Res = client.SendAsync(request).Result;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
                return Res;
            }

        }
    }


}

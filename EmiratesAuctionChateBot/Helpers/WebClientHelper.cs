using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public class WebClientHelper
    {
        public static async Task<HttpResponseMessage> ConsumeAsync(string baseurl, HttpMethod method, string relativeUrl, HttpContent content = null,
           string jsonObj = null, string lang = "", string basicAuthUser = "", string basicAuthPassword = "")
        {
            using (var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }))
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
                {
                    request.Content = new StringContent(jsonObj, Encoding.UTF8, "application/json");
                }
                if (content != null)
                {
                    request.Content = content;
                }
                var Res = new HttpResponseMessage();
                try
                {
                    Res = await client.SendAsync(request);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    throw ex;
                }
                return Res;
            }

        }

        public static string HandlePhoneFormat(string phone)
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                if (phone.StartsWith("+"))
                    phone = phone.Substring(1);
                if (phone.StartsWith("00"))
                    phone = phone.Substring(2);

                phone = string.Concat(phone.Where(c => !char.IsWhiteSpace(c)));
            }
            return phone;
        }
    }


}

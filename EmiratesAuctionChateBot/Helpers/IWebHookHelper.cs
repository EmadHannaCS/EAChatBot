using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public interface IWebHookHelper
    {
        Task<IRestResponse> sendTXTMsgAsync(string phone, string msg, string template = "");

    }
}

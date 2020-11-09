using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Helpers
{
    public interface IWebHookHelper
    {
        IRestResponse sendTXTMsg(string phone, string msg);

    }
}

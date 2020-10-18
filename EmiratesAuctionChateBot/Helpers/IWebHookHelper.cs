using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Helpers
{
    public interface IWebHookHelper
    {
        HttpResponseMessage sendTXTMsg(string phone, string msg);

    }
}

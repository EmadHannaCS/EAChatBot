using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class WebHookRequestBody
    {
        public List<destination> destinations { get; set; }
        public whatsApp whatsApp { get; set; }
    }
}

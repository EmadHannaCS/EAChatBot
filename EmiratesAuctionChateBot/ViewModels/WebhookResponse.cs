using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class WebhookResponse
    {
        public string _event { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string text { get; set; }
    }
}

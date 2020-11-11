using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class WebHookMessageVM
    {
        public string from { get; set; }
        public string to { get; set; }
        public MessageVM message { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class WebHookVM
    {
        public WebHookMessageVM[] results { get; set; }
        public int messageCount { get; set; }
        public int pendingMessageCount { get; set; }
    }
}

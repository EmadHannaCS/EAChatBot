using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class MessageVM
    {
        public string type { get; set; }
        public string text { get; set; }
        public decimal longitude { get; set; }
        public decimal latitude { get; set; }
    }
}

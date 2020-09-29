using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class AuctionInfoVM
    {
        public int lot { get; set; }
        public string currencyEn { get; set; }
        public string currencyAr { get; set; }
        public int currentPrice { get; set; }
    }
}

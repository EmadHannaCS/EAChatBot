﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class AuctionDetailsVM
    {
        public string SOPNumber { get; set; }
        public List<CarVM> Cars { get; set; }
        public double TotalAmount { get; set; }
    }
}

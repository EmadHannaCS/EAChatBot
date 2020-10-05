using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class RecoveryPriceVM
    {
        public List<LotPrice> LotPrices { get; set; }
        public int CountryId { get; set; }
        public string CountryNameEn { get; set; }
    }
}

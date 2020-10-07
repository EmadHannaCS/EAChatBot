using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ViewModels
{
    public class CarVM
    {
        public string makeEn { get; set; }
        public string makeAr { get; set; }
        public string modelEn { get; set; }
        public string modelAr { get; set; }
        public int DeliveryStatus { get; set; }
        public int RequireSelectHyaza { get; set; }
        public string BidderHyazaOrigin { get; set; }
        public CheckOutInfoVM CheckOutInfo { get; set; }
        public AuctionInfoVM AuctionInfo { get; set; }
        public int DeliveryRequestId { get; set; }


    }
}

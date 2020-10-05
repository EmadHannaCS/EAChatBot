using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class CheckoutDetailsVM
    {
        public RecoveryPriceVM RecoveryPrice { get; set; }
        public AddressVM AdressDetails { get; set; }

        public int UserPreferredTime { get; set; }

    }
}

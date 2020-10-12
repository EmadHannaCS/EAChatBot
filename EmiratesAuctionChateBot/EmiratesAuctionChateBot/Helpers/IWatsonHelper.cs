using IBM.Watson.Assistant.v2.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmiratesAuctionChateBot.Helpers
{
    public interface IWatsonHelper
    {
        MessageResponse Consume(string phone, string Text = "", bool isStart = false, bool isNormalChat = false);
        string ToEnglishNumber(string input);
        List<KeyValuePair<int, string>> GetChoises(string message);
    }
}

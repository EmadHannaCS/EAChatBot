using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Helpers;
using Helpers.WebClent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ViewModels;

namespace EmiratesAuctionChateBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatBotController : ControllerBase
    {
        private const string APIBaseUrl = "https://api.eas.ae/v2/";
        private readonly IHttpContextAccessor _httpContextAccessor;

        //public ChatBotController(IHttpContextAccessor httpContextAccessor)
        //{
        //    this._httpContextAccessor = httpContextAccessor;
        //}

        //To start chat call this api
        [HttpGet("StartChat")]
        public Task ChatBot(string authToken, string AuctionId, string phone)
        {
            //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["ChatId"];

            //Response.Cookies.Append("ChatId", authToken);



            string APIUrl = $"checkout/cars//getauctiondetails?auctionid={AuctionId}&authtoken={authToken}&source = androidphone";

            var result = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, APIUrl);

            AuctionDetailsVM auctionDetails = JsonSerializer.Deserialize<AuctionDetailsVM>(result.Content.ReadAsStringAsync().Result);

            var watsonResult = WatsonHelper.Consume();

            string message = watsonResult.generic[0].Replace("SOPCode", auctionDetails.SOPNumber);
            string carOption = "{0} lot# {1} with the price of {2} {3} ";
            for (int i = 0; i < auctionDetails.Cars.Count; i++)
            {
                var car = auctionDetails.Cars[i];
                if (car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1)
                {
                    message += Environment.NewLine + i + 1 + "-" + string.Format(carOption, car.makeEn + " " + car.modelEn, car.AuctionInfo.lot, car.AuctionInfo.currencyEn, car.AuctionInfo.currentPrice) + Environment.NewLine;
                }
            }

            WebHookHelper.sendTXTMsg(phone, message);

            return null;
        }

        //[HttpGet]
        //public object TestWatson()
        //{

        //    var response = WatsonHelper.Consume();
        //    return response.Response;
        //}
    }
}

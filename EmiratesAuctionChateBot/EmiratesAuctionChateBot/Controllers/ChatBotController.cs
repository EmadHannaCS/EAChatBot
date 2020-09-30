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
        private Dictionary<int, string> Emirates = new Dictionary<int, string>(new List<KeyValuePair<int, string>>()
        {
            new KeyValuePair<int, string>(1,"Abu Dhabi"),
            new KeyValuePair<int, string>(2,"Dubai"),
            new KeyValuePair<int, string>(3,"Sharja"),
            new KeyValuePair<int, string>(4,"Ras Al Khaimah"),
            new KeyValuePair<int, string>(5,"Fujairah"),
            new KeyValuePair<int, string>(6,"Ajman"),
            new KeyValuePair<int, string>(7,"Umm Al Quwian")

        });

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



            string APIUrl = $"checkout/cars/getauctiondetails?auctionid={AuctionId}&authtoken={authToken}&source = androidphone";

            var result = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, APIUrl);

            AuctionDetailsVM auctionDetails = JsonSerializer.Deserialize<AuctionDetailsVM>(result.Content.ReadAsStringAsync().Result);

            var watsonResult = WatsonHelper.Consume("hello");

            string message = watsonResult.Output.Generic[0].Text.Replace("SOPCode", auctionDetails.SOPNumber);
            string carOption = "{0} lot# {1} with the price of {2} {3} ";
            var cars = auctionDetails.Cars.Where(car => car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1).ToList();
            for (int i = 0; i < auctionDetails.Cars.Count; i++)
            {
                var car = auctionDetails.Cars[i];
                message += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeEn + " " + car.modelEn, car.AuctionInfo.lot, car.AuctionInfo.currencyEn, car.AuctionInfo.currentPrice);

            }

            WebHookHelper.sendTXTMsg(phone, message);

            for (int i = 0; i < auctionDetails.Cars.Count; i++)
            {
                var car = auctionDetails.Cars[i];

                if (car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1)
                {
                    message = watsonResult.Output.Generic[1].Text.Replace("CarNum", car.makeEn + " " + car.modelEn).Replace("number", car.AuctionInfo.lot.ToString()).
                        Replace("currency", car.AuctionInfo.currencyEn).Replace("price", car.AuctionInfo.currentPrice.ToString());

                    message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                        Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                    WebHookHelper.sendTXTMsg(phone, message);

                    watsonResult = WatsonHelper.Consume("5");
                    message = watsonResult.Output.Generic[0].Text;
                    if (message.Contains("please select from choices "))
                    {
                        WebHookHelper.sendTXTMsg(phone, message);
                    }
                    else
                    {
                        message = message.Replace("number", "5").Replace("country", Emirates.GetValueOrDefault(5));
                        WebHookHelper.sendTXTMsg(phone, message);

                        watsonResult = WatsonHelper.Consume("nothing");
                        message = watsonResult.Output.Generic[0].Text;
                        if (message.Contains("please type yes or no "))
                        {
                            WebHookHelper.sendTXTMsg(phone, message);
                        }
                        else if (watsonResult.Output.Entities[0].Value.Contains("no"))
                        {
                            message = watsonResult.Output.Generic[1].Text.Replace("CarNum", car.makeEn + " " + car.modelEn).Replace("number", car.AuctionInfo.lot.ToString()).
                        Replace("currency", car.AuctionInfo.currencyEn).Replace("price", car.AuctionInfo.currentPrice.ToString());

                            message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                                Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                            WebHookHelper.sendTXTMsg(phone, message);
                        }

                    }



                }


            }




            return null;
        }


        //[HttpPost("ReceiveMessages")]
        //public Task ChatBot(dynamic data)
        //{

        //}
    }
}

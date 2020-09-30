using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EmiratesAuctionChateBot.Helpers;
using Helpers;
using IBM.Watson.Assistant.v2.Model;
using Microsoft.AspNetCore.Mvc;
using ViewModels;

namespace EmiratesAuctionChateBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatBotController : ControllerBase
    {

        private const string APIBaseUrl = "https://api.eas.ae/v2/";
        private AuctionDetailsVM auctionDetails;
        private MessageResponse watsonResult;
        private string UserPhone = string.Empty;
        private int Step = 0;
        private readonly IWatsonHelper _watsonHelper;
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

        private KeyValuePair<int, string> CurrentStep = new KeyValuePair<int, string>();




        public ChatBotController(IWatsonHelper watsonHelper)
        {
            _watsonHelper = watsonHelper;
        }

        [HttpGet("StartChat")]
        public Task ChatBot(string authToken, string AuctionId, string phone)
        {

            UserPhone = phone;


            string APIUrl = $"checkout/cars/getauctiondetails?auctionid={AuctionId}&authtoken={authToken}&source = androidphone";

            var result = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, APIUrl);

            auctionDetails = JsonSerializer.Deserialize<AuctionDetailsVM>(result.Content.ReadAsStringAsync().Result);


            watsonResult = _watsonHelper.Consume(phone, "hello", true);

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
                    WebHookHelper.sendTXTMsg(UserPhone, message);

                }


            }

            return null;
        }


        [HttpPost("ReceiveMessages")]
        public Task ChatBot(object data)
        {
            WebhookResponse Message = JsonSerializer.Deserialize<WebhookResponse>(data.ToString());


            switch (CurrentStep.Key)
            {
                case 1:
                    {

                        break;
                    }

                default:
                    {
                        break;
                    }

            }

            for (int i = 0; i < auctionDetails.Cars.Count; i++)
            {
                var car = auctionDetails.Cars[i];

                if (car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1)
                {
                    watsonResult = _watsonHelper.Consume("5");

                    CurrentStep = new KeyValuePair<int, string>(1, "choose emirate");

                    string message = watsonResult.Output.Generic[0].Text;
                    if (message.Contains("please select from choices "))
                    {
                        WebHookHelper.sendTXTMsg(UserPhone, message);
                    }
                    else
                    {
                        message = message.Replace("number", "5").Replace("country", Emirates.GetValueOrDefault(5));
                        WebHookHelper.sendTXTMsg(UserPhone, message);

                        watsonResult = _watsonHelper.Consume("nothing");
                        message = watsonResult.Output.Generic[0].Text;
                        if (message.Contains("please type yes or no "))
                        {
                            WebHookHelper.sendTXTMsg(UserPhone, message);
                        }
                        else if (watsonResult.Output.Entities[0].Value.Contains("no"))
                        {
                            message = watsonResult.Output.Generic[1].Text.Replace("CarNum", car.makeEn + " " + car.modelEn).Replace("number", car.AuctionInfo.lot.ToString()).
                        Replace("currency", car.AuctionInfo.currencyEn).Replace("price", car.AuctionInfo.currentPrice.ToString());

                            message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                                Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                            WebHookHelper.sendTXTMsg(UserPhone, message);
                        }

                    }



                }


            }
            return null;
        }
    }
}

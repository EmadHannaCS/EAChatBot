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
        private string SelectedEmirate = string.Empty;
        private int Step = 1;
        private int CarNum = 0;
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

        private KeyValuePair<string, int> CurrentStep = new KeyValuePair<string, int>();

        private Dictionary<string, string> ChoosedEmirate = new Dictionary<string, string>();


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

            string message = watsonResult.Output.Generic[0].Text.Replace("{SOPCode}", auctionDetails.SOPNumber);
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
                    message = watsonResult.Output.Generic[1].Text.Replace("{CarNum}", car.makeEn + " " + car.modelEn).Replace("{number}", car.AuctionInfo.lot.ToString()).
                         Replace("{currency}", car.AuctionInfo.currencyEn).Replace("{price}", car.AuctionInfo.currentPrice.ToString());

                    message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                        Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                    WebHookHelper.sendTXTMsg(UserPhone, message);
                    CurrentStep = new KeyValuePair<string, int>(UserPhone, Step);

                }


            }

            return null;
        }


        [HttpPost("ReceiveMessages")]
        public Task ChatBot(object data)
        {
            WebhookResponse webHookMessage = JsonSerializer.Deserialize<WebhookResponse>(data.ToString());


            switch (CurrentStep.Value)
            {
                case 1:
                    {
                        for (int i = 0; i < auctionDetails.Cars.Count; i++)
                        {
                            var car = auctionDetails.Cars[i];
                            CarNum = i;
                            if (car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1)
                            {
                                watsonResult = _watsonHelper.Consume(webHookMessage.text);

                                string message = watsonResult.Output.Generic[0].Text;
                                if (message.Contains("please select from choices "))
                                {
                                    WebHookHelper.sendTXTMsg(UserPhone, message);
                                    CurrentStep = new KeyValuePair<string, int>(UserPhone, Step);
                                }
                                else
                                {
                                    SelectedEmirate = webHookMessage.text;
                                    ChoosedEmirate[UserPhone] = SelectedEmirate;
                                    message = message.Replace("{number}", webHookMessage.text).Replace("{country}", Emirates.GetValueOrDefault(int.Parse(webHookMessage.text)));
                                    WebHookHelper.sendTXTMsg(UserPhone, message);
                                    CurrentStep = new KeyValuePair<string, int>(UserPhone, Step++);

                                }

                            }


                        }
                        break;
                    }

                case 2:
                    {
                        var car = auctionDetails.Cars[CarNum];
                        watsonResult = _watsonHelper.Consume("nothing");
                        var message = watsonResult.Output.Generic[0].Text;
                        if (message.Contains("please type yes or no "))
                        {
                            WebHookHelper.sendTXTMsg(UserPhone, message);
                            CurrentStep = new KeyValuePair<string, int>(UserPhone, Step);
                        }
                        else if (watsonResult.Output.Entities[0].Value.Contains("no"))
                        {
                            message = watsonResult.Output.Generic[0].Text.Replace("{CarNum}", car.makeEn + " " + car.modelEn).Replace("{number}", car.AuctionInfo.lot.ToString()).
                        Replace("{currency}", car.AuctionInfo.currencyEn).Replace("{price}", car.AuctionInfo.currentPrice.ToString());

                            message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                                Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                            WebHookHelper.sendTXTMsg(UserPhone, message);
                            CurrentStep = new KeyValuePair<string, int>(UserPhone, 1);
                        }
                        else if (watsonResult.Output.Entities[0].Value.Contains("yes"))
                        {
                            SelectedEmirate = ChoosedEmirate[UserPhone];
                            message = watsonResult.Output.Generic[0].Text.Replace("{country}", Emirates.GetValueOrDefault(int.Parse(SelectedEmirate))).Replace("{lot}", car.AuctionInfo.lot.ToString());
                            WebHookHelper.sendTXTMsg(UserPhone, message);
                            CurrentStep = new KeyValuePair<string, int>(UserPhone, Step++);
                        }
                        break;
                    }

                default:
                    {
                        break;
                    }

            }


            return null;
        }
    }
}

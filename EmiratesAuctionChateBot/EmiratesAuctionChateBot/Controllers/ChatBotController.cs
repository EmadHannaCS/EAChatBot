using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BL.Managers;
using EmiratesAuctionChateBot.Helpers;
using Helpers;
using IBM.Watson.Assistant.v2.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ViewModels;

namespace EmiratesAuctionChateBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatBotController : ControllerBase
    {

        private const string APIBaseUrl = "https://api.eas.ae/v2/";
        private readonly ISessionsManager _sessionsManager;

        private static Dictionary<string, AuctionDetailsVM> UserAuctionDetails = new Dictionary<string, AuctionDetailsVM>();
        private static Dictionary<string, MessageResponse> UserWatsonResult = new Dictionary<string, MessageResponse>();
        private static Dictionary<string, string> UserSelectedEmirate = new Dictionary<string, string>();
        private static Dictionary<string, int> UserCarNum = new Dictionary<string, int>();
        private readonly IWatsonHelper _watsonHelper;
        private readonly IConfiguration _config;
        private static Dictionary<string, List<CarVM>> UserCars = new Dictionary<string, List<CarVM>>();
        private static Dictionary<string, string> UserAuthToken = new Dictionary<string, string>();
        private static Dictionary<string, string> UserAuctionId = new Dictionary<string, string>();
        private static Dictionary<int, string> Emirates = new Dictionary<int, string>(new List<KeyValuePair<int, string>>()
        {
            new KeyValuePair<int, string>(1,"Abu Dhabi"),
            new KeyValuePair<int, string>(2,"Dubai"),
            new KeyValuePair<int, string>(3,"Sharja"),
            new KeyValuePair<int, string>(4,"Ras Al Khaimah"),
            new KeyValuePair<int, string>(5,"Fujairah"),
            new KeyValuePair<int, string>(6,"Ajman"),
            new KeyValuePair<int, string>(7,"Umm Al Quwian")

        });




        public ChatBotController(IWatsonHelper watsonHelper, ISessionsManager sessionsManager, IConfiguration config)
        {
            _sessionsManager = sessionsManager;
            _watsonHelper = watsonHelper;
            _config = config;
        }

        [HttpGet("StartChat")]
        public Task ChatBot(string authToken, string AuctionId, string phone)
        {

            try
            {
                phone = WebClientHelper.HandlePhoneFormat(phone);

                UserAuctionId[phone] = AuctionId;
                UserAuthToken[phone] = authToken;

                string APIUrl = $"checkout/cars/getauctiondetails?auctionid={AuctionId}&authtoken={authToken}&source = androidphone";

                var result = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, APIUrl);

                UserAuctionDetails[phone] = JsonSerializer.Deserialize<AuctionDetailsVM>(result.Content.ReadAsStringAsync().Result);


                UserWatsonResult[phone] = _watsonHelper.Consume(phone, "hello", true);

                string message = UserWatsonResult[phone].Output.Generic[0].Text.Replace("{SOPCode}", UserAuctionDetails[phone].SOPNumber);
                string carOption = "{0} lot# {1} with the price of {2} {3} ";
                //var cars = UserAuctionDetails[phone].Cars.Where(car => car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1).ToList();
                for (int i = 0; i < UserAuctionDetails[phone].Cars.Count; i++)
                {
                    var car = UserAuctionDetails[phone].Cars[i];
                    message += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeEn + " " + car.modelEn, car.AuctionInfo.lot, car.AuctionInfo.currencyEn, car.AuctionInfo.currentPrice);

                }

                WebHookHelper.sendTXTMsg(phone, message);

                message = string.Empty;

                UserCars[phone] = new List<CarVM>(UserAuctionDetails[phone].Cars);
                var cars = UserAuctionDetails[phone].Cars;
                var carsCount = cars.Count;
                for (int i = 0; i < carsCount; i++)
                {
                    var car = cars[i];
                    if (car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1)
                    {
                        UserCarNum[phone] = i;
                        message = UserWatsonResult[phone].Output.Generic[1].Text.Replace("{CarNum}", car.makeEn + " " + car.modelEn).Replace("{number}", car.AuctionInfo.lot.ToString()).
                             Replace("{currency}", car.AuctionInfo.currencyEn).Replace("{price}", car.AuctionInfo.currentPrice.ToString());

                        message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                            Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";

                        UserCars[phone].Remove(car);
                        break;
                    }
                    else if (car.DeliveryStatus != 1 && car.CheckOutInfo.HasSourceLocation == 1 && car.CheckOutInfo.AllowDeliveryRequest == 1)
                    {
                        UserCarNum[phone] = i;
                        _sessionsManager.UpdateSessionStep(phone, 2);
                        UserCars[phone].Remove(car);
                        break;
                    }

                }
                WebHookHelper.sendTXTMsg(phone, message);
                _sessionsManager.UpdateSessionStep(phone);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return null;
        }


        [HttpPost("ReceiveMessages")]
        [Consumes("application/x-www-form-urlencoded")]
        public Task ChatBot([FromForm] object data)
        {
            try
            {
                var webHookMessage = JsonSerializer.Deserialize<WebhookResponse>(this.HttpContext.Request.Form["data"].ToString());

                var senderPhone = _config.GetValue<string>("SenderPhone");
                if (senderPhone == webHookMessage.from)
                {
                    return null;
                }
                var userStep = _sessionsManager.GetSession(webHookMessage.from)?.LatestResponseStep;
                switch (userStep)
                {
                    case 1:
                        {

                            var car = UserAuctionDetails[webHookMessage.from].Cars[UserCarNum[webHookMessage.from]];
                            if (car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1)
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.text);

                                string message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                if (message.Contains("please select from choices"))
                                {
                                    WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                                }
                                else
                                {
                                    UserSelectedEmirate[webHookMessage.from] = webHookMessage.text;
                                    message = message.Replace("{number}", webHookMessage.text).Replace("{country}", Emirates.GetValueOrDefault(int.Parse(webHookMessage.text)));
                                    WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                                    _sessionsManager.UpdateSessionStep(webHookMessage.from);
                                }

                            }



                            break;
                        }

                    case 2:
                        {
                            var car = UserAuctionDetails[webHookMessage.from].Cars[UserCarNum[webHookMessage.from]];
                            UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.text);
                            var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                            if (message.Contains("please type yes or no"))
                            {
                                WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                            }
                            else if (UserWatsonResult[webHookMessage.from].Output.Entities[0].Value.Contains("no"))
                            {
                                message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text.Replace("{CarNum}", car.makeEn + " " + car.modelEn).Replace("{number}", car.AuctionInfo.lot.ToString()).
                            Replace("{currency}", car.AuctionInfo.currencyEn).Replace("{price}", car.AuctionInfo.currentPrice.ToString());

                                message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                                    Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                                WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                                _sessionsManager.UpdateSessionStep(webHookMessage.from, 1);
                            }
                            else if (UserWatsonResult[webHookMessage.from].Output.Entities[0].Value.Contains("yes"))
                            {
                                message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text.Replace("{country}", Emirates.GetValueOrDefault(int.Parse(UserSelectedEmirate[webHookMessage.from]))).Replace("{lot}", car.AuctionInfo.lot.ToString());
                                WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                                _sessionsManager.UpdateSessionStep(webHookMessage.from);
                            }
                            break;
                        }

                    case 3:
                        {
                            if (webHookMessage.text.Contains("https://www.google.com/maps/place"))
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, "1");
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                                _sessionsManager.UpdateSessionStep(webHookMessage.from);
                            }
                            else
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                WebHookHelper.sendTXTMsg(webHookMessage.from, message);

                            }
                            break;
                        }

                    case 4:
                        {
                            UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.text);
                            var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                            message += Environment.NewLine + "1- 9:00AM - 1:00PM" + Environment.NewLine + "2- 1:00PM - 5:00PM" + Environment.NewLine + "3- 5:00PM - 9:00PM";
                            WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                            _sessionsManager.UpdateSessionStep(webHookMessage.from);
                            break;
                        }

                    case 5:
                        {
                            UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.text);
                            var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                            if (message.Contains("please choose from choices"))
                            {
                                WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                            }
                            else
                            {
                                WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                                _sessionsManager.UpdateSessionStep(webHookMessage.from);
                            }

                            break;
                        }

                    case 6:
                        {
                            UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.text);
                            var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                            var nextCars = UserCars[webHookMessage.from].Where(c => string.IsNullOrEmpty(c.BidderHyazaOrigin) && c.RequireSelectHyaza == 1);
                            if (nextCars.Any())
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, "1");
                                message = message.Replace("{lot}", nextCars.FirstOrDefault().AuctionInfo.lot.ToString());
                            }
                            else
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                            }
                            WebHookHelper.sendTXTMsg(webHookMessage.from, message);
                            ChatBot(UserAuthToken[webHookMessage.from], UserAuctionId[webHookMessage.from], webHookMessage.to);

                            break;
                        }



                    default:
                        {
                            break;
                        }

                }

            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            return null;
        }
    }
}

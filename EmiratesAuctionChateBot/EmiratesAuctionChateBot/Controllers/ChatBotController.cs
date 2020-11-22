using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BL.Managers;
using EmiratesAuctionChateBot.Helpers;
using Geocoding;
using Geocoding.Google;
using Helpers;
using IBM.Watson.Assistant.v2.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ViewModels;

namespace EmiratesAuctionChateBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatBotController : ControllerBase
    {

        private readonly string APIBaseUrl = string.Empty;

        private readonly ISessionsManager _sessionsManager;
        private readonly IWebHookHelper _webHookHelper;

        public static Dictionary<string, Dictionary<long, string>> choices = new Dictionary<string, Dictionary<long, string>>();
        private static Dictionary<string, AuctionDetailsVM> UserAuctionDetails = new Dictionary<string, AuctionDetailsVM>();
        private static Dictionary<string, MessageResponse> UserWatsonResult = new Dictionary<string, MessageResponse>();
        private static Dictionary<string, string> UserSelectedEmirate = new Dictionary<string, string>();
        private static Dictionary<string, bool> UserAlreadyInStep = new Dictionary<string, bool>();//to handle multi whatsapp msg in same time
        private static Dictionary<string, bool> UserIsInNormalChat = new Dictionary<string, bool>();
        private static Dictionary<string, bool> isStartChat = new Dictionary<string, bool>();
        private static Dictionary<string, string> userLanguage = new Dictionary<string, string>();
        private static Dictionary<int, string> DeliveryTimes = new Dictionary<int, string>(new List<KeyValuePair<int, string>>() {
        new KeyValuePair<int, string>(1,"9:00AM - 1:00PM"),
        new KeyValuePair<int, string>(2,"1:00PM - 5:00PM"),
        new KeyValuePair<int, string>(3,"5:00PM - 9:00PM")

        });


        private static Dictionary<string, string> userDeliveryTimes = new Dictionary<string, string>();

        private static Dictionary<string, int> UserCarNum = new Dictionary<string, int>();
        private readonly IWatsonHelper _watsonHelper;
        private readonly IConfiguration _config;
        private static Dictionary<string, List<CarVM>> UserCars = new Dictionary<string, List<CarVM>>();
        private static Dictionary<string, string> UserAuthToken = new Dictionary<string, string>();
        private static Dictionary<string, string> UserAuctionId = new Dictionary<string, string>();
        private static Dictionary<long, KeyValuePair<long, string>> EmiratesEN = new Dictionary<long, KeyValuePair<long, string>>(new List<KeyValuePair<long, KeyValuePair<long, string>>>()
        {
            new KeyValuePair<long, KeyValuePair<long,string>>(1,new KeyValuePair<long, string>(121,"Abu Dhabi")),
            new KeyValuePair<long, KeyValuePair<long,string>>(2,new KeyValuePair<long, string>(120,"Dubai")),
            new KeyValuePair<long, KeyValuePair<long,string>>(3,new KeyValuePair<long, string>(122,"Sharja")),
            new KeyValuePair<long, KeyValuePair<long,string>>(4,new KeyValuePair<long, string>(124,"Ras Al Khaimah")),
            new KeyValuePair<long, KeyValuePair<long,string>>(5,new KeyValuePair<long, string>(149,"Fujairah")),
            new KeyValuePair<long, KeyValuePair<long,string>>(6,new KeyValuePair<long, string>(123,"Ajman")),
            new KeyValuePair<long, KeyValuePair<long,string>>(7,new KeyValuePair<long, string>(125,"Umm Al Quwian"))

        });


        private static Dictionary<long, KeyValuePair<long, string>> EmiratesAR = new Dictionary<long, KeyValuePair<long, string>>(new List<KeyValuePair<long, KeyValuePair<long, string>>>()
        {
            new KeyValuePair<long, KeyValuePair<long,string>>(1,new KeyValuePair<long, string>(121,"ابو ظبي")),
            new KeyValuePair<long, KeyValuePair<long,string>>(2,new KeyValuePair<long, string>(120,"دبي")),
            new KeyValuePair<long, KeyValuePair<long,string>>(3,new KeyValuePair<long, string>(122,"الشارقه")),
            new KeyValuePair<long, KeyValuePair<long,string>>(4,new KeyValuePair<long, string>(124,"راس الخيمه")),
            new KeyValuePair<long, KeyValuePair<long,string>>(5,new KeyValuePair<long, string>(149,"الفجيره")),
            new KeyValuePair<long, KeyValuePair<long,string>>(6,new KeyValuePair<long, string>(123,"عجمان")),
            new KeyValuePair<long, KeyValuePair<long,string>>(7,new KeyValuePair<long, string>(125,"ام القيوان"))

        });




        public ChatBotController(IWatsonHelper watsonHelper, ISessionsManager sessionsManager, IConfiguration config, IWebHookHelper webHookHelper)
        {
            _sessionsManager = sessionsManager;
            _watsonHelper = watsonHelper;
            _config = config;
            _webHookHelper = webHookHelper;
            APIBaseUrl = _config.GetValue<string>("APISBaseUrl");
        }

        [HttpGet("StartChat")]
        public async Task<ActionResult<ResponseVM>> ChatBot(string authToken, string auctionId, string phone, bool firstCall = true, bool isInitiated = false, bool isEnd = false)
        {

            try
            {

                phone = WebClientHelper.HandlePhoneFormat(phone);

                UserIsInNormalChat[phone] = false;

                UserAuctionId[phone] = auctionId;
                UserAuthToken[phone] = authToken;
                UserAlreadyInStep[phone] = false;

                if (firstCall)
                {
                    string APIUrl = $"checkout/cars/getauctiondetails?auctionid={auctionId}&authtoken={authToken}&source=mweb";

                    var result = await WebClientHelper.ConsumeAsync(APIBaseUrl, HttpMethod.Get, APIUrl);

                    UserAuctionDetails[phone] = JsonSerializer.Deserialize<AuctionDetailsVM>(result.Content.ReadAsStringAsync().Result);

                    if (UserAuctionDetails[phone].Cars.Where(car => (car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1) || (car.DeliveryStatus != 1 && car.CheckOutInfo.HasSourceLocation == 1 && car.CheckOutInfo.AllowDeliveryRequest == 1)).Any())
                    {
                        await _webHookHelper.sendTXTMsgAsync(phone, " ", "bid_message_cars");

                        _watsonHelper.Consume(phone);

                        _sessionsManager.UpdateSessionStep(phone, 1);
                        return Ok(new ResponseVM()
                        {
                            StatusCode = StatusCodes.Status200OK,
                            Messgae = "Initation success"
                        });
                    }

                }
                #region ForTest
                //for (int i = 0; i < UserAuctionDetails[phone].Cars.Count; i++)
                //{
                //    var car = UserAuctionDetails[phone].Cars[i];
                //    if (i % 2 == 0)
                //    {
                //        car.BidderHyazaOrigin = string.Empty;
                //        car.RequireSelectHyaza = 1;
                //    }
                //    else
                //    {
                //        car.DeliveryStatus = 0;
                //        car.CheckOutInfo.HasSourceLocation = 1;
                //        car.CheckOutInfo.AllowDeliveryRequest = 1;
                //    }
                //}

                #endregion


                bool send = false;
                //UserWatsonResult[phone] = _watsonHelper.Consume(phone, isEnd ? "" : "1", true);

                string firstMessage = string.Empty;
                if (isInitiated || isEnd)
                {
                    if (UserCars.ContainsKey(phone))
                        UserCars[phone].Clear();

                    send = false;


                    UserWatsonResult[phone] = _watsonHelper.Consume(phone, "1");

                    send = true;

                    firstMessage += Environment.NewLine + UserWatsonResult[phone].Output.Generic[0].Text;

                    string carOption = _watsonHelper.Consume(phone).Output.Generic[0].Text;

                    for (int i = 0; i < UserAuctionDetails[phone].Cars?.Count; i++)
                    {
                        var car = UserAuctionDetails[phone].Cars[i];
                        string carOptionReplace = string.Empty;
                        if (userLanguage[phone] == "en")
                        {
                            carOptionReplace = carOption.Replace("{{ Car Type}}", car.makeEn + " " + car.modelEn).Replace("{{Lot number}}", car.AuctionInfo.lot.ToString());
                        }
                        else
                        {
                            carOptionReplace = carOption.Replace("{{ Car Type}}", car.makeAr + " " + car.modelAr).Replace("{{Lot number}}", car.AuctionInfo.lot.ToString());
                        }
                        firstMessage += Environment.NewLine + (i + 1) + "-" + carOptionReplace;

                    }


                    if (userLanguage[phone] == "en")
                    {
                        firstMessage += Environment.NewLine + "Lets finalize some few steps";
                    }
                    else
                    {
                        firstMessage += Environment.NewLine + "لننتهي من بعض الخطوات";
                    }



                }
                string secondMessage = string.Empty;
                if (!UserCars.ContainsKey(phone) || UserCars[phone] == null || UserCars[phone].Count == 0)
                {
                    UserCars[phone] = new List<CarVM>(UserAuctionDetails[phone].Cars ?? new List<CarVM>());
                }
                var cars = UserCars[phone];
                var carsCount = cars.Count;
                if (!isEnd)
                {
                    var userHyazaCars = cars.Where(car => car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1).ToList();
                    for (int i = 0; i < carsCount; i++)
                    {
                        var car = cars[i];
                        if (car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1)
                        {
                            send = true;
                            UserCarNum[phone] = i;
                            secondMessage = _watsonHelper.Consume(phone, "2").Output.Generic[0].Text;
                            if (userLanguage[phone] == "en")
                            {
                                secondMessage = secondMessage.Replace("{{ Car Type}}", car.makeEn + " " + car.modelEn).Replace("{{ Lot Number }}", car.AuctionInfo.lot.ToString());
                            }
                            else
                            {
                                secondMessage = secondMessage.Replace("{{ Car Type}}", car.makeAr + " " + car.modelAr).Replace("{{ Lot Number }}", car.AuctionInfo.lot.ToString());

                            }
                            if (userLanguage[phone] == "en")
                            {
                                secondMessage += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                                    Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                            }
                            else
                            {
                                secondMessage += Environment.NewLine + "1- ابو ظبي" + Environment.NewLine + "2- دبي" + Environment.NewLine + "3- الشارقه" + Environment.NewLine + "4- راس الخيمه" +
                                                                    Environment.NewLine + "5- الفجيره" + Environment.NewLine + "6- عجمان" + Environment.NewLine + "7- ام القيوان";
                            }

                            _sessionsManager.UpdateSessionStep(phone);

                            break;
                        }


                    }
                    if (userHyazaCars.Count == 0)
                    {
                        var deliveryCars = cars.Where(car => car.DeliveryStatus != 1 && car.CheckOutInfo.HasSourceLocation == 1 && car.CheckOutInfo.AllowDeliveryRequest == 1).ToList();
                        if (deliveryCars.Count > 0)
                        {
                            send = true;
                            for (int i = 0; i < deliveryCars.Count; i++)
                            {
                                var car = deliveryCars[i];
                                UserCarNum[phone] = i;
                                // message += _watsonHelper.Consume(phone, "1").Output.Generic[0].Text.Replace("{CarNum}", car.makeEn + " " + car.modelEn).Replace("{lot}", car.AuctionInfo.lot.ToString());

                            }
                            secondMessage += _watsonHelper.Consume(phone, "1").Output.Generic[0].Text;

                        }
                        _sessionsManager.UpdateSessionStep(phone, 4);


                    }


                }


                if (send && !string.IsNullOrEmpty(secondMessage))
                {
                    //concat the 2 msgs in one
                    await _webHookHelper.sendTXTMsgAsync(phone, firstMessage);

                    _webHookHelper.sendTXTMsgAsync(phone, secondMessage);

                }


                if (isEnd || string.IsNullOrEmpty(secondMessage))
                {
                    UserIsInNormalChat[phone] = true;
                    _sessionsManager.UpdateSessionStep(phone, 0);
                }
                else
                {
                    UserIsInNormalChat[phone] = false;
                }


            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ResponseVM()
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Messgae = "An error occured please try again later"
                    });
            }

            return Ok(new ResponseVM() { Messgae = "Sent Succefully !", StatusCode = StatusCodes.Status200OK });
        }


        [HttpPost("ReceiveMessages")]
        //[Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult<ResponseVM>> ChatBot(/*[FromForm]*/ WebHookVM data)
        {
            var webHookMessage = data.results[0];
            try
            {
                if (!UserAlreadyInStep.ContainsKey(webHookMessage.from))
                    UserAlreadyInStep[webHookMessage.from] = false;

                if (!UserIsInNormalChat.ContainsKey(webHookMessage.from))
                    UserIsInNormalChat[webHookMessage.from] = true;


                if (!UserWatsonResult.ContainsKey(webHookMessage.from))
                    UserWatsonResult[webHookMessage.from] = new MessageResponse();


                if (!choices.ContainsKey(webHookMessage.from))
                    choices[webHookMessage.from] = new Dictionary<long, string>();

                if (!UserAlreadyInStep[webHookMessage.from])
                {
                    UserAlreadyInStep[webHookMessage.from] = true;
                    var senderPhone = _config.GetValue<string>("SenderPhone");
                    if (senderPhone == webHookMessage.from)
                    {
                        return StatusCode(StatusCodes.Status400BadRequest
                            , new ResponseVM()
                            {
                                StatusCode = StatusCodes.Status400BadRequest,
                                Messgae = "Sender is the same number as the phone registered for sending messages"
                            });
                    }
                    webHookMessage.message.text = _watsonHelper.ToEnglishNumber(webHookMessage.message.text);
                    var userStep = _sessionsManager.GetSession(webHookMessage.from)?.LatestResponseStep;

                    isStartChat[webHookMessage.from] = true;

                    if (userStep > 0)
                    {
                        isStartChat[webHookMessage.from] = false;

                    }


                    if (UserIsInNormalChat[webHookMessage.from])
                    {
                        if (choices[webHookMessage.from].Count > 1)
                        {
                            if (Char.IsDigit(webHookMessage.message.text, 0) && webHookMessage.message.text != "0")//0 is main menu in watson
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, choices[webHookMessage.from].GetValueOrDefault(int.Parse(webHookMessage.message.text))?.Trim(), isStartChat[webHookMessage.from], UserIsInNormalChat[webHookMessage.from]);
                            }
                            else
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text.Trim(), isStartChat[webHookMessage.from], UserIsInNormalChat[webHookMessage.from]);
                            }
                        }
                        else
                        {

                            UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text, isStartChat[webHookMessage.from], UserIsInNormalChat[webHookMessage.from]);
                        }

                        string message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;


                        _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                        var newChoices = _watsonHelper.GetChoises(message);
                        if (newChoices != null && newChoices.Count > 0)
                            choices[webHookMessage.from] = newChoices;

                        isStartChat[webHookMessage.from] = false;
                        _sessionsManager.UpdateSessionStep(webHookMessage.from);
                        UserAlreadyInStep[webHookMessage.from] = false;
                        return Ok(new ResponseVM()
                        {
                            StatusCode = StatusCodes.Status200OK
                            ,
                            Messgae = "Message Sent !"
                        });


                    }

                    switch (userStep)
                    {
                        case 1:
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);

                                string message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;

                                await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                if (!message.Contains("please select from languages"))
                                {
                                    if (UserWatsonResult[webHookMessage.from].Output.Intents[0].Intent == "Language_EN")
                                    {
                                        userLanguage[webHookMessage.from] = "en";
                                    }
                                    else
                                    {
                                        userLanguage[webHookMessage.from] = "ar";
                                    }
                                    await ChatBot(UserAuthToken[webHookMessage.from], UserAuctionId[webHookMessage.from], webHookMessage.from, false, true);
                                }
                                break;
                            }
                        case 2:
                            {

                                var car = UserCars[webHookMessage.from][UserCarNum[webHookMessage.from]];

                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);

                                string message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                if (message.Contains("Your choice doesn't seem valid. Please try again.") || message.Contains("الاختيار غير صحيح يرجي المحاوله مره اخري"))
                                {
                                    _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                }
                                else
                                {


                                    UserSelectedEmirate[webHookMessage.from] = webHookMessage.message.text;
                                    if (userLanguage[webHookMessage.from] == "en")
                                    {
                                        message = message.Replace("{{ Car type}}", car.makeEn + " " + car.modelEn)
                                            .Replace("{{Lot number}}", car.AuctionInfo.lot.ToString()).Replace("{{ Emirate name}}", EmiratesEN.GetValueOrDefault(long.Parse(webHookMessage.message.text)).Value);
                                    }
                                    else
                                    {
                                        message = message.Replace("{{ Car type}}", car.makeAr + " " + car.modelAr)
                                                                                    .Replace("{{Lot number}}", car.AuctionInfo.lot.ToString()).Replace("{{ Emirate name}}", EmiratesAR.GetValueOrDefault(long.Parse(webHookMessage.message.text)).Value);
                                    }
                                    _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                    _sessionsManager.UpdateSessionStep(webHookMessage.from);

                                }


                                break;
                            }

                        case 3:
                            {
                                var car = UserCars[webHookMessage.from][UserCarNum[webHookMessage.from]];
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;

                                if (message.ToLower().Contains("Please type".ToLower()) || message.ToLower().Contains("برجاء ارسال نعم او لا".ToLower()))
                                {
                                    _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                }
                                else if (UserWatsonResult[webHookMessage.from].Output.Entities[0].Value.Contains("no") || UserWatsonResult[webHookMessage.from].Output.Entities[0].Value.Contains("لا"))
                                {
                                    if (userLanguage[webHookMessage.from] == "en")
                                    {
                                        message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text.Replace("{{ Car Type}}", car.makeEn + " " + car.modelEn).Replace("{{ Lot Number }}", car.AuctionInfo.lot.ToString());

                                        message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                                            Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                                    }
                                    else
                                    {
                                        message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text.Replace("{{ Car Type}}", car.makeAr + " " + car.modelAr).Replace("{{ Lot Number }}", car.AuctionInfo.lot.ToString());

                                        message += Environment.NewLine + "1- ابو ظبي" + Environment.NewLine + "2- دبي" + Environment.NewLine + "3- الشارقه" + Environment.NewLine + "4- راس الخيمه" +
                                                                     Environment.NewLine + "5- الفجيره" + Environment.NewLine + "6- عجمان" + Environment.NewLine + "7- ام القيوان";

                                    }
                                    _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                    _sessionsManager.UpdateSessionStep(webHookMessage.from, 2);
                                }
                                else if (UserWatsonResult[webHookMessage.from].Output.Entities[0].Value.Contains("yes") || UserWatsonResult[webHookMessage.from].Output.Entities[0].Value.Contains("نعم"))
                                {
                                    using (var multiPartFormData = new MultipartFormDataContent())
                                    {

                                        multiPartFormData.Add(new StringContent(UserAuthToken[webHookMessage.from]), "authtoken");
                                        multiPartFormData.Add(new StringContent(car.AuctionInfo.lot.ToString()), "ciaid");
                                        if (userLanguage[webHookMessage.from] == "en")
                                        {
                                            multiPartFormData.Add(new StringContent(EmiratesEN.GetValueOrDefault(long.Parse(UserSelectedEmirate[webHookMessage.from])).Key.ToString()), "hayazaOriginId");
                                        }
                                        else
                                        {
                                            multiPartFormData.Add(new StringContent(EmiratesAR.GetValueOrDefault(long.Parse(UserSelectedEmirate[webHookMessage.from])).Key.ToString()), "hayazaOriginId");

                                        }
                                        var result = WebClientHelper.ConsumeAsync(APIBaseUrl, HttpMethod.Post, "carsonline/updatehyazaorigin?source=androidphone", multiPartFormData);

                                        if (userLanguage[webHookMessage.from] == "en")
                                        {
                                            message = message.
                                            Replace("{{ Car Type}}", car.makeEn + " " + car.modelEn).
                                            Replace("{{ Lot number}}", car.AuctionInfo.lot.ToString()).
                                            Replace("{{ Emriate}}", EmiratesEN.GetValueOrDefault(long.Parse(UserSelectedEmirate[webHookMessage.from])).Value);
                                        }
                                        else
                                        {
                                            message = message.
                                            Replace("{{ Car Type}}", car.makeAr + " " + car.modelAr).
                                            Replace("{{ Lot number}}", car.AuctionInfo.lot.ToString()).
                                            Replace("{{ Emriate}}", EmiratesAR.GetValueOrDefault(long.Parse(UserSelectedEmirate[webHookMessage.from])).Value);
                                        }
                                        _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                        UserCars[webHookMessage.from].Remove(car);


                                    }
                                    if (UserCars[webHookMessage.from].Where(car => car.BidderHyazaOrigin == string.Empty && car.RequireSelectHyaza == 1).Any())
                                    {
                                        _watsonHelper.Consume(webHookMessage.from, "1");
                                        _sessionsManager.UpdateSessionStep(webHookMessage.from, 1);
                                        await ChatBot(UserAuthToken[webHookMessage.from], UserAuctionId[webHookMessage.from], webHookMessage.from, false, false);

                                    }
                                    else
                                    {
                                        UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);

                                        message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                        _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);


                                        _sessionsManager.UpdateSessionStep(webHookMessage.from);

                                    }
                                }

                                break;
                            }

                        case 4:
                            {


                                //IGeocoder geocoder = new GoogleGeocoder() { ApiKey = "AIzaSyBUtZjrs_fplvEWPYljDM2e_yDwEWMpaTM" };

                                //var addresses = await geocoder.GeocodeAsync(webHookMessage.message.text);

                                if (webHookMessage.message.type == "LOCATION")
                                {


                                    var latitude = webHookMessage.message.latitude;
                                    var longitude = webHookMessage.message.longitude;

                                    string getrecoverypriceAPIUrl = $"checkout/cars/getrecoveryprice?GX={latitude}&GY={longitude}&authtoken={UserAuthToken[webHookMessage.from]}&invoiceId={UserAuctionDetails[webHookMessage.from].SOPId}&source=androidphone";
                                    var getrecoverypriceResult = await WebClientHelper.ConsumeAsync(APIBaseUrl, HttpMethod.Get, getrecoverypriceAPIUrl);
                                    var recoveryPrice = JsonSerializer.Deserialize<RecoveryPriceVM>(getrecoverypriceResult.Content.ReadAsStringAsync().Result);

                                    UserAuctionDetails[webHookMessage.from].CheckoutDetails = new CheckoutDetailsVM();
                                    UserAuctionDetails[webHookMessage.from].CheckoutDetails.RecoveryPrice = recoveryPrice;

                                    string getaddressdetailsfromgeoAPIUrl = $"checkout/cars/getaddressdetailsfromgeo?GX={latitude}&GY={longitude}&authtoken={UserAuthToken[webHookMessage.from]}&invoiceId={UserAuctionDetails[webHookMessage.from].SOPNumber}&source=androidphone";
                                    var getaddressdetailsfromgeoResult = await WebClientHelper.ConsumeAsync(APIBaseUrl, HttpMethod.Get, getaddressdetailsfromgeoAPIUrl);
                                    var adressDetails = JsonSerializer.Deserialize<AdressDetailsVM>(getaddressdetailsfromgeoResult.Content.ReadAsStringAsync().Result);

                                    UserAuctionDetails[webHookMessage.from].CheckoutDetails.AdressDetails = new AddressVM
                                    {
                                        AreaId = adressDetails.results.FirstOrDefault(c => c.field_id == "AreaId")?.value,
                                        CityId = adressDetails.results.FirstOrDefault(c => c.field_id == "CityId")?.value,
                                        StreetAddressEn = adressDetails.results.FirstOrDefault(c => c.field_id == "StreetAddressEn")?.value,
                                    };

                                    if (recoveryPrice.CountryId == 0 || string.IsNullOrWhiteSpace(UserAuctionDetails[webHookMessage.from].CheckoutDetails.AdressDetails.CityId))
                                    {
                                        UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                        var error = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                        _webHookHelper.sendTXTMsgAsync(webHookMessage.from, error);
                                        break;
                                    }

                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, "1");
                                    var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                    _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                    _sessionsManager.UpdateSessionStep(webHookMessage.from);
                                }
                                else
                                {
                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                    var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                    _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                }
                                break;
                            }

                        case 5:
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                _sessionsManager.UpdateSessionStep(webHookMessage.from);
                                break;
                            }

                        case 6:
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                if (message.Contains("Please type your choice.") || message.Contains("برجاء الاختيار من الاختيارات المتاحه"))
                                {
                                    _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                }
                                else
                                {
                                    userDeliveryTimes[webHookMessage.from] = DeliveryTimes[int.Parse(webHookMessage.message.text)];
                                    message = message.Replace("{Time Interval}", userDeliveryTimes[webHookMessage.from]);
                                    string carOption = "{0} lot number{1}";
                                    if (userLanguage[webHookMessage.from] == "en")
                                    {
                                        for (int i = 0; i < UserAuctionDetails[webHookMessage.from].Cars.Count; i++)
                                        {
                                            var car = UserAuctionDetails[webHookMessage.from].Cars[i];
                                            message += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeEn + " " + car.modelEn, car.AuctionInfo.lot);

                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < UserAuctionDetails[webHookMessage.from].Cars.Count; i++)
                                        {
                                            var car = UserAuctionDetails[webHookMessage.from].Cars[i];
                                            message += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeAr + " " + car.modelAr, car.AuctionInfo.lot);

                                        }
                                    }

                                    UserAuctionDetails[webHookMessage.from].CheckoutDetails.UserPreferredTime = (long.Parse(webHookMessage.message.text) - 1);
                                    await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);



                                    var checkoutDetails = UserAuctionDetails[webHookMessage.from].CheckoutDetails;
                                    var priceList = string.Join(",", checkoutDetails.RecoveryPrice?.LotPrices?.Select(c => c.LotNumber + "-" + c.Distance + "-" + c.Price)?.ToList() ?? new List<string>());

                                    string createdeliveryrequestAPIUrl = $"checkout/cars/createdeliveryrequest?CountryId={checkoutDetails.RecoveryPrice.CountryId}&CountryName={checkoutDetails.RecoveryPrice.CountryNameEn}&authtoken={UserAuthToken[webHookMessage.from]}&AreaId={checkoutDetails.AdressDetails.AreaId}&source=androidphone&NearestLandMark=&SpecialNotes={webHookMessage.message.text}&CityId={checkoutDetails.AdressDetails.CityId}&BuldingNo=&PreferredTime={checkoutDetails.UserPreferredTime}&invoiceId={UserAuctionDetails[webHookMessage.from].SOPId}&StreetAddressEn={checkoutDetails.AdressDetails.StreetAddressEn}&PriceList={priceList}";
                                    var createdeliveryrequest = await WebClientHelper.ConsumeAsync(APIBaseUrl, HttpMethod.Get, createdeliveryrequestAPIUrl);
                                    var createdeliveryrequestResult = JsonSerializer.Deserialize<object>(createdeliveryrequest.Content.ReadAsStringAsync().Result);

                                    string getdeliveryrequestforconfirmAPIUrl = $"checkout/cars/getdeliveryrequestforconfirm?authtoken={UserAuthToken[webHookMessage.from]}&source=androidphone&InvoiceID={UserAuctionDetails[webHookMessage.from].SOPId}";
                                    var getdeliveryrequestforconfirm = await WebClientHelper.ConsumeAsync(APIBaseUrl, HttpMethod.Get, getdeliveryrequestforconfirmAPIUrl);
                                    var getdeliveryrequestforconfirmResult = JsonSerializer.Deserialize<AuctionDetailsVM>(getdeliveryrequestforconfirm.Content.ReadAsStringAsync().Result);
                                    var requestIds = string.Join(",", getdeliveryrequestforconfirmResult.Cars.Select(c => c.DeliveryRequestId.ToString())?.ToList() ?? new List<string>());


                                    string confirmdeliveryrequestAPIUrl = $"checkout/cars/confirmdeliveryrequest?authtoken={UserAuthToken[webHookMessage.from]}&source=androidphone&InvoiceID={UserAuctionDetails[webHookMessage.from].SOPId}&deliveryRequestIds={requestIds}";
                                    var confirmdeliveryrequest = await WebClientHelper.ConsumeAsync(APIBaseUrl, HttpMethod.Get, confirmdeliveryrequestAPIUrl);
                                    var confirmdeliveryrequestResult = JsonSerializer.Deserialize<object>(confirmdeliveryrequest.Content.ReadAsStringAsync().Result);


                                    string APIUrl = $"checkout/cars/getauctiondetails?auctionid={UserAuctionId[webHookMessage.from]}&authtoken={UserAuthToken[webHookMessage.from]}&source=mweb";

                                    var result = await WebClientHelper.ConsumeAsync(APIBaseUrl, HttpMethod.Get, APIUrl);

                                    UserAuctionDetails[webHookMessage.from] = JsonSerializer.Deserialize<AuctionDetailsVM>(result.Content.ReadAsStringAsync().Result);


                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                    message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;

                                    message = message.Replace("{{Outstanding Balance}}", UserAuctionDetails[webHookMessage.from].TotalAmount.ToString())
                                        .Replace("{date}", userDeliveryTimes[webHookMessage.from]);

                                    await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);




                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, UserAuctionDetails[webHookMessage.from].TotalAmount.ToString());

                                    message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;

                                    if (UserAuctionDetails[webHookMessage.from].TotalAmount > 500)
                                    {
                                        System.Timers.Timer timer = new System.Timers.Timer(50000);
                                        timer.AutoReset = true;
                                        timer.Enabled = true;

                                        await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                        timer.Stop();
                                        timer.Dispose();

                                        UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                        message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                        await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                        UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                        message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                        message = message.Replace("{{ Time Internaval }}", userDeliveryTimes[webHookMessage.from]);
                                        carOption = "{0} lot number{1}";

                                        if (userLanguage[webHookMessage.from] == "en")
                                        {
                                            for (int i = 0; i < UserAuctionDetails[webHookMessage.from].Cars.Count; i++)
                                            {
                                                var car = UserAuctionDetails[webHookMessage.from].Cars[i];
                                                message += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeEn + " " + car.modelEn, car.AuctionInfo.lot);

                                            }
                                        }
                                        else
                                        {
                                            for (int i = 0; i < UserAuctionDetails[webHookMessage.from].Cars.Count; i++)
                                            {
                                                var car = UserAuctionDetails[webHookMessage.from].Cars[i];
                                                message += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeAr + " " + car.modelAr, car.AuctionInfo.lot);

                                            }
                                        }
                                        await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                        UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                        message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;

                                        await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                        _sessionsManager.UpdateSessionStep(webHookMessage.from, null);


                                    }
                                    else
                                    {
                                        message = message.Replace("{{Outstanding Balance}}", UserAuctionDetails[webHookMessage.from].TotalAmount.ToString());
                                        _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                        _sessionsManager.UpdateSessionStep(webHookMessage.from);

                                    }

                                }

                                break;
                            }

                        case 7:
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);
                                if (webHookMessage.message.text == "1")
                                {
                                    var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                    await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                }
                                else if (webHookMessage.message.text == "2")
                                {
                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, "600");

                                    System.Timers.Timer timer = new System.Timers.Timer(50000);
                                    timer.AutoReset = true;
                                    timer.Enabled = true;


                                    var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                    timer.Stop();
                                    timer.Dispose();

                                    await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);
                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                    message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                    await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                    message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                    message = message.Replace("{{ Time Internaval }}", userDeliveryTimes[webHookMessage.from]);
                                    string carOption = "{0} lot number{1}";

                                    if (userLanguage[webHookMessage.from] == "en")
                                    {
                                        for (int i = 0; i < UserAuctionDetails[webHookMessage.from].Cars.Count; i++)
                                        {
                                            var car = UserAuctionDetails[webHookMessage.from].Cars[i];
                                            message += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeEn + " " + car.modelEn, car.AuctionInfo.lot);

                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < UserAuctionDetails[webHookMessage.from].Cars.Count; i++)
                                        {
                                            var car = UserAuctionDetails[webHookMessage.from].Cars[i];
                                            message += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeAr + " " + car.modelAr, car.AuctionInfo.lot);

                                        }
                                    }
                                    await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);

                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                    message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;

                                    await _webHookHelper.sendTXTMsgAsync(webHookMessage.from, message);


                                }
                                _sessionsManager.UpdateSessionStep(webHookMessage.from, null);
                                break;
                            }
                        default:
                            {
                                break;
                            }

                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseVM()
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Messgae = "An error occured please try again later !"
                });
            }
            UserAlreadyInStep[webHookMessage.from] = false;

            return Ok(new ResponseVM()
            {
                StatusCode = StatusCodes.Status200OK,
                Messgae = "Successfully Sent !"
            });
        }


    }
}

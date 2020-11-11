﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
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


        private static Dictionary<string, int> UserCarNum = new Dictionary<string, int>();
        private readonly IWatsonHelper _watsonHelper;
        private readonly IConfiguration _config;
        private static Dictionary<string, List<CarVM>> UserCars = new Dictionary<string, List<CarVM>>();
        private static Dictionary<string, string> UserAuthToken = new Dictionary<string, string>();
        private static Dictionary<string, string> UserAuctionId = new Dictionary<string, string>();
        private static Dictionary<long, KeyValuePair<long, string>> Emirates = new Dictionary<long, KeyValuePair<long, string>>(new List<KeyValuePair<long, KeyValuePair<long, string>>>()
        {
            new KeyValuePair<long, KeyValuePair<long,string>>(1,new KeyValuePair<long, string>(121,"Abu Dhabi")),
            new KeyValuePair<long, KeyValuePair<long,string>>(2,new KeyValuePair<long, string>(120,"Dubai")),
            new KeyValuePair<long, KeyValuePair<long,string>>(3,new KeyValuePair<long, string>(122,"Sharja")),
            new KeyValuePair<long, KeyValuePair<long,string>>(4,new KeyValuePair<long, string>(124,"Ras Al Khaimah")),
            new KeyValuePair<long, KeyValuePair<long,string>>(5,new KeyValuePair<long, string>(149,"Fujairah")),
            new KeyValuePair<long, KeyValuePair<long,string>>(6,new KeyValuePair<long, string>(123,"Ajman")),
            new KeyValuePair<long, KeyValuePair<long,string>>(7,new KeyValuePair<long, string>(125,"Umm Al Quwian"))

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
        public Task ChatBot(string authToken, string auctionId, string phone, bool firstCall = true, bool isEnd = false)
        {

            try
            {



                phone = WebClientHelper.HandlePhoneFormat(phone);

                UserIsInNormalChat[phone] = true;

                UserAuctionId[phone] = auctionId;
                UserAuthToken[phone] = authToken;
                UserAlreadyInStep[phone] = false;

                string APIUrl = $"checkout/cars/getauctiondetails?auctionid={auctionId}&authtoken={authToken}&source=mweb";

                var result = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, APIUrl);

                UserAuctionDetails[phone] = JsonSerializer.Deserialize<AuctionDetailsVM>(result.Content.ReadAsStringAsync().Result);

                #region ForTest
                //UserAuctionDetails[phone].Cars[0].RequireSelectHyaza = 0;
                //UserAuctionDetails[phone].Cars[0].DeliveryStatus = 0;
                //UserAuctionDetails[phone].Cars[0].CheckOutInfo.AllowDeliveryRequest = 1;
                //UserAuctionDetails[phone].Cars[0].CheckOutInfo.HasSourceLocation = 1;

                #endregion


                bool send = false;
                UserWatsonResult[phone] = _watsonHelper.Consume(phone, isEnd ? "" : "1", true);

                string firstMessage = string.Empty;
                if (firstCall || isEnd)
                {
                    if (UserCars.ContainsKey(phone))
                        UserCars[phone].Clear();
                    firstMessage = UserWatsonResult[phone].Output.Generic[0].Text.Replace("{SOPCode}", UserAuctionDetails[phone].SOPNumber).Replace("{TotalAmount}", UserAuctionDetails[phone].TotalAmount.ToString());

                    send = false;
                    string carOption = "{0} lot# {1} with the price of {2} {3} ";
                    for (int i = 0; i < UserAuctionDetails[phone].Cars?.Count; i++)
                    {
                        send = true;
                        var car = UserAuctionDetails[phone].Cars[i];
                        firstMessage += Environment.NewLine + (i + 1) + "-" + string.Format(carOption, car.makeEn + " " + car.modelEn, car.AuctionInfo.lot, car.AuctionInfo.currencyEn, car.AuctionInfo.currentPrice);

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

                            UserCarNum[phone] = i;
                            secondMessage = _watsonHelper.Consume(phone).Output.Generic[0].Text;
                            secondMessage = secondMessage.Replace("{CarNum}", car.makeEn + " " + car.modelEn).Replace("{number}", car.AuctionInfo.lot.ToString()).
                                 Replace("{currency}", car.AuctionInfo.currencyEn).Replace("{price}", car.AuctionInfo.currentPrice.ToString());

                            secondMessage += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                                Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";

                            UserCars[phone].Remove(car);
                            _sessionsManager.UpdateSessionStep(phone);

                            break;
                        }


                    }
                    if (userHyazaCars.Count == 0)
                    {
                        var deliveryCars = cars.Where(car => car.DeliveryStatus != 1 && car.CheckOutInfo.HasSourceLocation == 1 && car.CheckOutInfo.AllowDeliveryRequest == 1).ToList();
                        if (deliveryCars.Count > 0)
                        {
                            for (int i = 0; i < deliveryCars.Count; i++)
                            {
                                var car = deliveryCars[i];
                                UserCarNum[phone] = i;
                                // message += _watsonHelper.Consume(phone, "1").Output.Generic[0].Text.Replace("{CarNum}", car.makeEn + " " + car.modelEn).Replace("{lot}", car.AuctionInfo.lot.ToString());

                                UserCars[phone].Remove(car);
                            }
                            secondMessage += _watsonHelper.Consume(phone, "1").Output.Generic[0].Text;

                        }
                        _sessionsManager.UpdateSessionStep(phone, 3);


                    }


                }
                if (send && !string.IsNullOrEmpty(secondMessage))
                {
                    //concat the 2 msgs in one
                    var msg = firstMessage + Environment.NewLine + secondMessage;
                    _webHookHelper.sendTXTMsg(phone, msg);
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
            }

            return null;
        }


        [HttpPost("ReceiveMessages")]
        //[Consumes("application/x-www-form-urlencoded")]
        public Task ChatBot(/*[FromForm]*/ WebHookVM data)
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
                        return null;
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


                        _webHookHelper.sendTXTMsg(webHookMessage.from, message);

                        var newChoices = _watsonHelper.GetChoises(message);
                        if (newChoices != null && newChoices.Count > 0)
                            choices[webHookMessage.from] = newChoices;

                        isStartChat[webHookMessage.from] = false;
                        _sessionsManager.UpdateSessionStep(webHookMessage.from);
                        UserAlreadyInStep[webHookMessage.from] = false;
                        return null;


                    }

                    switch (userStep)
                    {
                        case 1:
                            {

                                var car = UserAuctionDetails[webHookMessage.from].Cars[UserCarNum[webHookMessage.from]];

                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);

                                string message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                if (message.Contains("Your choice doesn't seem valid. Please try again."))
                                {
                                    _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                }
                                else
                                {


                                    UserSelectedEmirate[webHookMessage.from] = webHookMessage.message.text;
                                    message = message.Replace("{number}", webHookMessage.message.text).Replace("{country}", Emirates.GetValueOrDefault(long.Parse(webHookMessage.message.text)).Value);
                                    _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                    _sessionsManager.UpdateSessionStep(webHookMessage.from);
                                }


                                break;
                            }

                        case 2:
                            {
                                var car = UserAuctionDetails[webHookMessage.from].Cars[UserCarNum[webHookMessage.from]];



                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;

                                if (message.ToLower().Contains("please type"))
                                {
                                    _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                }
                                else if (UserWatsonResult[webHookMessage.from].Output.Entities[0].Value.Contains("no"))
                                {
                                    message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text.Replace("{CarNum}", car.makeEn + " " + car.modelEn).Replace("{number}", car.AuctionInfo.lot.ToString()).
                                Replace("{currency}", car.AuctionInfo.currencyEn).Replace("{price}", car.AuctionInfo.currentPrice.ToString());

                                    message += Environment.NewLine + "1- Abu Dhabi" + Environment.NewLine + "2- Dubai" + Environment.NewLine + "3- Sharja" + Environment.NewLine + "4- Ras Al Khaimah" +
                                        Environment.NewLine + "5- Fujairah" + Environment.NewLine + "6- Ajman" + Environment.NewLine + "7- Umm Al Quwian";
                                    _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                    _sessionsManager.UpdateSessionStep(webHookMessage.from, 1);
                                }
                                else if (UserWatsonResult[webHookMessage.from].Output.Entities[0].Value.Contains("yes"))
                                {
                                    using (var multiPartFormData = new MultipartFormDataContent())
                                    {

                                        multiPartFormData.Add(new StringContent(UserAuthToken[webHookMessage.from]), "authtoken");
                                        multiPartFormData.Add(new StringContent(car.AuctionInfo.lot.ToString()), "ciaid");
                                        multiPartFormData.Add(new StringContent(Emirates.GetValueOrDefault(long.Parse(UserSelectedEmirate[webHookMessage.from])).Key.ToString()), "hayazaOriginId");
                                        var result = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Post, "carsonline/updatehyazaorigin?source=androidphone", multiPartFormData);

                                    }
                                    if (UserCars[webHookMessage.from].Count > 0)
                                    {
                                        ChatBot(UserAuthToken[webHookMessage.from], UserAuctionId[webHookMessage.from], webHookMessage.from, false);

                                    }
                                    else
                                    {
                                        message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text.Replace("{country}", Emirates.GetValueOrDefault(long.Parse(UserSelectedEmirate[webHookMessage.from])).Value).Replace("{lot}", car.AuctionInfo.lot.ToString());
                                        _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                        _sessionsManager.UpdateSessionStep(webHookMessage.from);

                                    }
                                }

                                break;
                            }

                        case 3:
                            {
                                if (webHookMessage.message.type == "LOCATION")
                                {

                                    var latitude = webHookMessage.message.latitude;
                                    var longitude = webHookMessage.message.longitude;

                                    _webHookHelper.sendTXTMsg(webHookMessage.from, "Processing...");

                                    string getrecoverypriceAPIUrl = $"checkout/cars/getrecoveryprice?GX={latitude}&GY={longitude}&authtoken={UserAuthToken[webHookMessage.from]}&invoiceId={UserAuctionDetails[webHookMessage.from].SOPId}&source=androidphone";
                                    var getrecoverypriceResult = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, getrecoverypriceAPIUrl);
                                    var recoveryPrice = JsonSerializer.Deserialize<RecoveryPriceVM>(getrecoverypriceResult.Content.ReadAsStringAsync().Result);

                                    UserAuctionDetails[webHookMessage.from].CheckoutDetails = new CheckoutDetailsVM();
                                    UserAuctionDetails[webHookMessage.from].CheckoutDetails.RecoveryPrice = recoveryPrice;

                                    string getaddressdetailsfromgeoAPIUrl = $"checkout/cars/getaddressdetailsfromgeo?GX={latitude}&GY={longitude}&authtoken={UserAuthToken[webHookMessage.from]}&invoiceId={UserAuctionDetails[webHookMessage.from].SOPNumber}&source=androidphone";
                                    var getaddressdetailsfromgeoResult = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, getaddressdetailsfromgeoAPIUrl);
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
                                        _webHookHelper.sendTXTMsg(webHookMessage.from, error);
                                        break;
                                    }

                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, "1");
                                    var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                    _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                    _sessionsManager.UpdateSessionStep(webHookMessage.from);
                                }
                                else
                                {
                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                    var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                    _webHookHelper.sendTXTMsg(webHookMessage.from, message);

                                }
                                break;
                            }

                        case 4:
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                message += Environment.NewLine + "1- 9:00AM - 1:00PM" + Environment.NewLine + "2- 1:00PM - 5:00PM" + Environment.NewLine + "3- 5:00PM - 9:00PM";
                                _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                _sessionsManager.UpdateSessionStep(webHookMessage.from);
                                break;
                            }

                        case 5:
                            {
                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                if (message.Contains("please choose from choices"))
                                {
                                    _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                }
                                else
                                {
                                    UserAuctionDetails[webHookMessage.from].CheckoutDetails.UserPreferredTime = (long.Parse(webHookMessage.message.text) - 1);
                                    _webHookHelper.sendTXTMsg(webHookMessage.from, message);
                                    _sessionsManager.UpdateSessionStep(webHookMessage.from);
                                }

                                break;
                            }

                        case 6:
                            {


                                UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, webHookMessage.message.text);
                                var message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;
                                _webHookHelper.sendTXTMsg(webHookMessage.from, message);

                                var nextCars = UserCars[webHookMessage.from].Where(c => (string.IsNullOrEmpty(c.BidderHyazaOrigin) && c.RequireSelectHyaza == 1)
                                || (c.DeliveryStatus != 1 && c.CheckOutInfo.HasSourceLocation == 1 && c.CheckOutInfo.AllowDeliveryRequest == 1));
                                if (nextCars != null && nextCars.Any())
                                {
                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from, "1");
                                    message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text.Replace("{lot}", nextCars.FirstOrDefault().AuctionInfo.lot.ToString());
                                }
                                else
                                {
                                    UserWatsonResult[webHookMessage.from] = _watsonHelper.Consume(webHookMessage.from);
                                    message = UserWatsonResult[webHookMessage.from].Output.Generic[0].Text;

                                    _webHookHelper.sendTXTMsg(webHookMessage.from, "Processing...");
                                    var checkoutDetails = UserAuctionDetails[webHookMessage.from].CheckoutDetails;
                                    var priceList = string.Join(",", checkoutDetails.RecoveryPrice?.LotPrices?.Select(c => c.LotNumber + "-" + c.Distance + "-" + c.Price)?.ToList() ?? new List<string>());

                                    string createdeliveryrequestAPIUrl = $"checkout/cars/createdeliveryrequest?CountryId={checkoutDetails.RecoveryPrice.CountryId}&CountryName={checkoutDetails.RecoveryPrice.CountryNameEn}&authtoken={UserAuthToken[webHookMessage.from]}&AreaId={checkoutDetails.AdressDetails.AreaId}&source=androidphone&NearestLandMark=&SpecialNotes={webHookMessage.message.text}&CityId={checkoutDetails.AdressDetails.CityId}&BuldingNo=&PreferredTime={checkoutDetails.UserPreferredTime}&invoiceId={UserAuctionDetails[webHookMessage.from].SOPId}&StreetAddressEn={checkoutDetails.AdressDetails.StreetAddressEn}&PriceList={priceList}";
                                    var createdeliveryrequest = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, createdeliveryrequestAPIUrl);
                                    var createdeliveryrequestResult = JsonSerializer.Deserialize<object>(createdeliveryrequest.Content.ReadAsStringAsync().Result);

                                    string getdeliveryrequestforconfirmAPIUrl = $"checkout/cars/getdeliveryrequestforconfirm?authtoken={UserAuthToken[webHookMessage.from]}&source=androidphone&InvoiceID={UserAuctionDetails[webHookMessage.from].SOPId}";
                                    var getdeliveryrequestforconfirm = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, getdeliveryrequestforconfirmAPIUrl);
                                    var getdeliveryrequestforconfirmResult = JsonSerializer.Deserialize<AuctionDetailsVM>(getdeliveryrequestforconfirm.Content.ReadAsStringAsync().Result);
                                    var requestIds = string.Join(",", getdeliveryrequestforconfirmResult.Cars.Select(c => c.DeliveryRequestId.ToString())?.ToList() ?? new List<string>());


                                    string confirmdeliveryrequestAPIUrl = $"checkout/cars/confirmdeliveryrequest?authtoken={UserAuthToken[webHookMessage.from]}&source=androidphone&InvoiceID={UserAuctionDetails[webHookMessage.from].SOPId}&deliveryRequestIds={requestIds}";
                                    var confirmdeliveryrequest = WebClientHelper.Consume(APIBaseUrl, HttpMethod.Get, confirmdeliveryrequestAPIUrl);
                                    var confirmdeliveryrequestResult = JsonSerializer.Deserialize<object>(confirmdeliveryrequest.Content.ReadAsStringAsync().Result);

                                }
                                _webHookHelper.sendTXTMsg(webHookMessage.from, message);

                                _sessionsManager.UpdateSessionStep(webHookMessage.from, null);

                                ChatBot(UserAuthToken[webHookMessage.from], UserAuctionId[webHookMessage.from], webHookMessage.from, false, true);

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
            }
            UserAlreadyInStep[webHookMessage.from] = false;

            return null;
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Managers;
using Microsoft.AspNetCore.Mvc;

namespace EmiratesAuctionChateBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ISessionsManager _sessionsManager;
        public TestController(ISessionsManager sessionsManager)
        {
            _sessionsManager = sessionsManager;
        }
        [HttpGet("session")]
        public string session()
        {
            _sessionsManager.SetSession("00201285416464", "ggg");
            var sId = _sessionsManager.GetSession("00201285416464");
            return sId;
        }
    }
}

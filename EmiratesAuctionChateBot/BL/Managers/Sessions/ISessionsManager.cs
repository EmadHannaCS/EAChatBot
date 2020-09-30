using System;
using System.Collections.Generic;
using System.Text;

namespace BL.Managers
{
    public interface ISessionsManager
    {
        public string GetSession(string phone);
        public void SetSession(string phone, string sessionId);
    }
}

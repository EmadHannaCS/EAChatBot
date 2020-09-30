using DAL.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace BL.Managers
{
    public interface ISessionsManager
    {
        public UserSession GetSession(string phone);
        public void SetSession(string phone, string sessionId);

        public void UpdateSessionStep(string phone, int? step = null);
    }
}


using DAL.DB;
using DAL.Repository;
using DAL.UnitOfWork;
using Helpers.WebClent;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DAL.Managers
{
    public class SessionsManager : ISessionsManager
    {
        private readonly IUnitOfWork _uow;
        private readonly IRepository<UserSession> _repo;

        private static List<UserSession> _sessions;


        public SessionsManager(IUnitOfWork unit)
        {
            _uow = unit;
            _repo = unit.GetRepository<UserSession>();
            if (_sessions == null)
                _sessions = new List<UserSession>();
        }
        public string GetSession(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;
            UserSession session = null;
            try
            {
                session = _sessions.FirstOrDefault(c => c.UserPhone == phone);
                if (session == null) //check db
                {
                    session = _repo.FirstOrDefault(c => c.UserPhone == phone);
                    if (session != null)//add in list
                    {
                        _sessions.Add(session);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            return session?.LastSessionId;
        }

        public void SetSession(string phone, string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(sessionId))
            {
                try
                {
                    var session = _repo.FirstOrDefault(c => c.UserPhone == phone);
                    if (session == null)//add in db
                    {
                        session = new UserSession
                        {
                            CreatedAt = DateTime.Now,
                            LastSessionId = sessionId,
                            UserPhone = phone
                        };
                        _repo.Add(session);
                    }
                    else
                    {
                        session.ModifiedAt = DateTime.Now;
                        session.LastSessionId = sessionId;
                    }
                    _uow.Commit();

                    var staticSession = _sessions.FirstOrDefault(c => c.UserPhone == phone);
                    if (staticSession == null)
                    {
                        _sessions.Add(session);
                    }
                    else
                    {
                        staticSession.ModifiedAt = DateTime.Now;
                        staticSession.LastSessionId = sessionId;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }
        }
    }
}

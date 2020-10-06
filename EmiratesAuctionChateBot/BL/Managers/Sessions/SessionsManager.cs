
using DAL.DB;
using DAL.Repository;
using DAL.UnitOfWork;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BL.Managers
{
    public class SessionsManager : ISessionsManager
    {
        private readonly IUnitOfWork _uow;
        private readonly IRepository<UserSession> _repo;

        //private static List<UserSession> _sessions;


        public SessionsManager(IUnitOfWork unit)
        {
            _uow = unit;
            _repo = unit.GetRepository<UserSession>();
            //if (_sessions == null)
            //    _sessions = new List<UserSession>();
        }
        public UserSession GetSession(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;
            UserSession session = null;
            try
            {
                //session = _sessions.FirstOrDefault(c => c.UserPhone == phone);
                if (session == null) //check db
                {
                    session = _repo.FirstOrDefault(c => c.UserPhone == phone);
                    //if (session != null)//add in list
                    //{
                    //    _sessions.Add(session);
                    //}
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            return session;
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
                            UserPhone = phone,
                            LatestResponseStep = 0
                        };
                        _repo.Add(session);
                    }
                    else
                    {
                        session.ModifiedAt = DateTime.Now;
                        session.LastSessionId = sessionId;
                        session.LatestResponseStep = 0;

                    }
                    _uow.Commit();

                    //var staticSession = _sessions.FirstOrDefault(c => c.UserPhone == phone);
                    //if (staticSession == null)
                    //{
                    //    _sessions.Add(session);
                    //}
                    //else
                    //{
                    //    staticSession = session;
                    //}
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }
        }

        public void UpdateSessionStep(string phone, int? step = null)
        {
            var session = _repo.FirstOrDefault(c => c.UserPhone == phone);
            if (session != null)
            {
                session.LatestResponseStep = step ?? session.LatestResponseStep + 1;
                _uow.Commit();

                //var staticSession = _sessions.FirstOrDefault(c => c.UserPhone == phone);
                //if (staticSession == null)
                //{
                //    _sessions.Add(session);
                //}
                //else
                //{
                //    staticSession = session;
                //}

            }

        }
    }
}

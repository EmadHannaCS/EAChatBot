using log4net;
using System;
using System.Reflection;

namespace Helpers.WebClent
{
    public class LogHelper
    {
        static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void LogException(Exception ex)
        {
            LogException(ex.Message, ex.StackTrace);
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                LogException(ex.Message, ex.StackTrace);
            }
        }
        public static void LogException(string msg, string stackTrace)
        {
            log.Error($"Exception Msg: {msg}");
            log.Error($"Exception StackTrace: {stackTrace}");
        }

        public static void LogInfo(string msg)
        {
            log.Info(msg);
        }
    }
}

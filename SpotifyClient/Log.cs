using Anotar.NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyClient
{
    public class Log
    {
        public static void Trace(object blah, string message)
        {
            //LogTo.Trace(message);
        }

        public static void Trace(object blah, string message, params object[] format)
        {
            //LogTo.Trace(message, format);
        }

        public static void Debug(object blah, string message)
        {
            //LogTo.Debug(message);
        }

        public static void Debug(object blah, string message, params object[] format)
        {
            //LogTo.Debug(message, format);
        }

        public static void Info(object blah, string message)
        {
            LogTo.Info(message);
        }

        public static void Info(object blah, string message, params object[] format)
        {
            LogTo.Info(message, format);
        }

        public static void Warning(object blah, string message)
        {
            LogTo.Warn(message);
        }

        public static void Warning(object blah, string message, params object[] format)
        {
            LogTo.Warn(message, format);
        }

        public static void Error(object blah, string message)
        {
            LogTo.Error(message);
        }

        public static void Error(object blah, string message, params object[] format)
        {
            LogTo.Error(message, format);
        }
    }
}

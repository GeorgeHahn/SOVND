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
        { }
        public static void Trace(object blah, string message, params object[] format)
        { }
        public static void Debug(object blah, string message)
        { }
        public static void Debug(object blah, string message, params object[] format)
        { }
        public static void Info(object blah, string message)
        { }
        public static void Info(object blah, string message, params object[] format)
        { }
        public static void Warning(object blah, string message)
        { }
        public static void Warning(object blah, string message, params object[] format)
        { }
        public static void Error(object blah, string message)
        { }
        public static void Error(object blah, string message, params object[] format)
        { }
    }

    public enum Plugin
    {
        LOG_MODULE
    }
}

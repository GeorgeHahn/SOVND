using System;
using HipchatApiV2.Enums;

namespace SOVND.Server.Utils
{
    public class HipchatLogger
    {
        private const string Roomname = "Server logs";
        private const RoomColors DebugColor = RoomColors.Gray;
        private const RoomColors WarnColor = RoomColors.Yellow;
        private const RoomColors InfoColor = RoomColors.Purple;
        private const RoomColors ErrorColor = RoomColors.Red;
        private const RoomColors FatalColor = RoomColors.Red;

        public void Debug(string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), DebugColor);
        }
        public void Debug(Exception exception, string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), DebugColor);
            HipchatSender.SendNotification(Roomname, exception.Message, DebugColor);
            HipchatSender.SendNotification(Roomname, exception.StackTrace, DebugColor);
        }
        public bool IsDebugEnabled { get { return true; } }
        public void Information(string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), WarnColor);
        }
        public void Information(Exception exception, string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), WarnColor);
            HipchatSender.SendNotification(Roomname, exception.Message, WarnColor);
            HipchatSender.SendNotification(Roomname, exception.StackTrace, WarnColor);
        }
        public bool IsInformationEnabled { get { return true; } }
        public void Warning(string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), InfoColor);
        }
        public void Warning(Exception exception, string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), InfoColor);
            HipchatSender.SendNotification(Roomname, exception.Message, InfoColor);
            HipchatSender.SendNotification(Roomname, exception.StackTrace, InfoColor);
        }
        public bool IsWarningEnabled { get { return true; } }
        public void Error(string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), ErrorColor);
        }
        public void Error(Exception exception, string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), ErrorColor);
            HipchatSender.SendNotification(Roomname, exception.Message, ErrorColor);
            HipchatSender.SendNotification(Roomname, exception.StackTrace, ErrorColor);
        }
        public bool IsErrorEnabled { get { return true; } }
        public void Fatal(string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), FatalColor);
        }
        public void Fatal(Exception exception, string format, params object[] args)
        {
            HipchatSender.SendNotification(Roomname, string.Format(format, args), FatalColor);
            HipchatSender.SendNotification(Roomname, exception.Message, FatalColor);
            HipchatSender.SendNotification(Roomname, exception.StackTrace, FatalColor);
        }
        public bool IsFatalEnabled { get { return true; } }
    }
}
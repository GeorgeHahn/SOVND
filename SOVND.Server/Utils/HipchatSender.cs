using System;
using Anotar.NLog;
using HipchatApiV2;
using HipchatApiV2.Enums;
using HipchatApiV2.Exceptions;

namespace SOVND.Server.Utils
{
    public static class HipchatSender
    {
        private static readonly HipchatClient _hipchat;

        static HipchatSender()
        {
            _hipchat = new HipchatClient();
        }

        public static void SendNotification(string roomName, string message, RoomColors color)
        {
            retry:
            try
            {
                _hipchat.SendNotification(roomName, message, color);
            }
            catch (HipchatWebException e) if (e.Message.Contains("Room not found"))
            {
                _hipchat.CreateRoom(roomName, false, null, RoomPrivacy.Private);
                goto retry;
            }
            catch (HipchatWebException e) if (e.Message.Contains("rate"))
            {
                // Fail silently
            }
            catch (Exception e)
            {
                LogTo.Error("Hipchat error: {0}- {1}\r\n{2}", e.GetType().ToString(), e.Message, e.StackTrace);
            }
        }
    }
}
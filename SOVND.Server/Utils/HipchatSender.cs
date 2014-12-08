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
            catch (HipchatWebException e) if (e.Message.Contains("404"))
            {
                _hipchat.CreateRoom(roomName, false, null, RoomPrivacy.Private);
                goto retry;
            }
            catch (HipchatWebException e) if (e.Message.Contains("rate limit"))
            {
                // Fail silently
            }
        }
    }
}
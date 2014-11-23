using SOVND.Lib;

namespace SOVND.Client.Settings
{
    public class SovndMqttSettings : IMQTTSettings
    {
        public string Broker { get { return "104.131.87.42"; } }

        public int Port { get { return 8883; } }


        public string Username
        {
            get { return "georgehahn"; }
        }

        public string Password
        {
            get { return ""; }
        }
    }
}
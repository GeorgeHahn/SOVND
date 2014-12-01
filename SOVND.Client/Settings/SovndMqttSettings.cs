using SOVND.Lib;
using SOVND.Lib.Models;
using SOVND.Lib.Settings;

namespace SOVND.Client.Settings
{
    public class SovndMqttSettings : IMQTTSettings
    {
        private readonly ISettingsProvider _settings;

        public SovndMqttSettings(ISettingsProvider settings)
        {
            _settings = settings;
        }

        public string Broker { get { return "104.131.87.42"; } }

        public int Port { get { return 8883; } }


        public string Username
        {
            get { return _settings.GetAuthSettings().SOVNDUsername; }
        }

        public string Password
        {
            get { return _settings.GetAuthSettings().SOVNDPassword; }
        }
    }
}
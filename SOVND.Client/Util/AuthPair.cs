using SOVND.Lib.Models;
using SOVND.Lib.Settings;

namespace SOVND.Client.Util
{
    public class AuthPair
    {
        public IMQTTSettings ConnectionSettings { get; private set; }
        public ISettingsProvider Settings { get; private set; }

        public AuthPair(IMQTTSettings connectionSettings, ISettingsProvider settings)
        {
            ConnectionSettings = connectionSettings;
            Settings = settings;
        }
    }
}
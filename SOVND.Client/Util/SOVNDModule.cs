using Charlotte;

namespace SOVND.Client.Util
{
    public class SOVNDModule : MqttModule
    {
        public SOVNDModule(AuthPair pair)
            : base(pair.ConnectionSettings.Broker, pair.ConnectionSettings.Port, pair.Settings.GetSettings().SOVNDUsername, pair.Settings.GetSettings().SOVNDPassword)
        { }
    }
}
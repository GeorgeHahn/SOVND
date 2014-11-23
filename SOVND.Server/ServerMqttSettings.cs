using SOVND.Lib;
using System.IO;

namespace SOVND.Server
{
    public class ServerMqttSettings : IMQTTSettings
    {
        public string Broker { get { return "104.131.87.42"; } }

        public int Port { get { return 8883; } }

        public string Username
        {
            get { return File.ReadAllText("username.bin"); }
        }

        public string Password
        {
            get { return File.ReadAllText("password.bin"); }
        }
    }
}
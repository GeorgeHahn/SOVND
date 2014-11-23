using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOVND.Lib
{
    public interface IMQTTSettings
    {
        string Broker { get; }
        int Port { get; }

        string Username { get; }
        string Password { get; }
    }

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

    public class ServerMqttSettings : IMQTTSettings
    {
        public string Broker { get { return "104.131.87.42"; } }

        public int Port { get { return 8883; } }

        public string Username
        {
            get { return ""; }
        }

        public string Password
        {
            get { return ""; }
        }
    }

    public interface IListeningSettings
    {
        string Channel { get; set; }
    }

    public class SovndListeningSettings : IListeningSettings
    {
        public string Channel
        {
            get { return "ambient"; }
            set { throw new NotImplementedException(); }
        }
    }
}

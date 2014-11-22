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
        string Username { get; set; }
        string Password { get; set; }
        int Port { get; set; }
    }

    public class SovndMqttSettings : IMQTTSettings
    {
        public string Broker
        {
            get { return "127.0.0.1"; }
        }

        public string Username
        {
            get { return "georgehahn"; }
            set { throw new NotImplementedException(); }
        }

        public string Password
        {
            get { return ""; }
            set { throw new NotImplementedException(); }
        }

        public int Port
        {
            get { return 1883; }
            set { throw new NotImplementedException(); }
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

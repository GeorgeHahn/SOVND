using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOVND.Server.Settings
{
    public class RedisProvider
    {
        private readonly ConnectionMultiplexer _redis;

        public ConnectionMultiplexer redis
        {
            get { return _redis; }
        }

        public RedisProvider()
        {
            _redis = ConnectionMultiplexer.Connect("127.0.0.1");
        }
    }
}

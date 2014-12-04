using CSRedis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOVND.Server.Settings
{
    public class RedisProvider
    {
        private readonly RedisClient _redis;

        public RedisClient redis
        {
            get { return _redis; }
        }

        public RedisProvider()
        {
            _redis = new RedisClient("localhost");
        }
    }
}

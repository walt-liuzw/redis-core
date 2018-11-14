using System;
using System.Collections.Generic;
using System.Text;
using CSRedis;

namespace RedisCore.Subscribe
{
    public class SubscribeProvider
    {
        private string connectionStr;
        public SubscribeProvider(string connectionStr)
        {
            this.connectionStr = connectionStr;
        }

        public void SubscribeList(string channal, string message)
        {
            using (var client = new CSRedisClient(connectionStr))
            {
                client.SubscribeList(channal, msg =>
                {
                    var logMsg = $"{channal}:{msg}";

                });
            }
        }
        public void SubscribeListBroadcast(string channal, string consumer, string message)
        {
            using (var client = new CSRedisClient(connectionStr))
            {
              client.SubscribeListBroadcast(channal, consumer, msg =>
              {
                  var logMsg = $"{channal}:{msg}";

              });
            }


        }
        public void Subscribe(string channal, string message)
        {
            using (var client = new CSRedisClient(connectionStr))
            {
                client.SubscribeList(channal, msg =>
                {
                    var logMsg = $"{channal}:{msg}";

                });
            }


        }
    }
}

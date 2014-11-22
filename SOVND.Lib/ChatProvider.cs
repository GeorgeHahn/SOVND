using System;
using System.Collections.ObjectModel;
using Charlotte;

namespace SOVND.Lib
{
    public class ChatProvider : MqttModule
    {
        private Channel _channel;
        public Action<string> Log = _ => Console.WriteLine(_);

        public ObservableCollection<ChatMessage> Chats = new ObservableCollection<ChatMessage>();

        public ChatProvider(Channel channel)
            : base("127.0.0.1", 1883, "", "")
        {
            _channel = channel;
                
            // Channel chats
            On["/\{channel.MQTTName}/chat"] = _ =>
            {
                Log("\{channel.Name} chat - \{_.Message}");

                if (SyncHolder.sync != null)
                    SyncHolder.sync.Send((x) => Chats.Add(new ChatMessage(_.Message)), null);
                else
                    Chats.Add(new ChatMessage(_.Message));
            };

            Run();
        }
    }
}
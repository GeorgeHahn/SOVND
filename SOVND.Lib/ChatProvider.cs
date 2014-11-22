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

        public void Subscribe(Channel channel)
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

        public ChatProvider(IMQTTSettings settings)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        { }
    }
}
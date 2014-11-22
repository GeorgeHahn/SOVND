using System;
using System.Collections.ObjectModel;
using Charlotte;

namespace SOVND.Lib
{
    public interface IChatProvider
    {
        ObservableCollection<ChatMessage> Chats { get; }

        void Subscribe(ChannelHandler channel);

        //void Unsubscribe();
    }

    public class ChatProvider : MqttModule, IChatProvider
    {
        private ChannelHandler _channel;
        public Action<string> Log = _ => Console.WriteLine(_);

        public ObservableCollection<ChatMessage> Chats { get; } = new ObservableCollection<ChatMessage>();

        public void Subscribe(ChannelHandler channel)
        {
            _channel = channel;

            // ChannelHandler chats
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
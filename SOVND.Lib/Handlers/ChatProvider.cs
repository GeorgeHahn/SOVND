using System.Collections.ObjectModel;
using Anotar.NLog;
using Charlotte;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public interface IChatProvider
    {
        ObservableCollection<ChatMessage> Chats { get; }

        void Subscribe(ChannelHandler channel);

        //void Unsubscribe();
    }

    public class ChatProvider : MqttModule, IChatProvider
    {
        private readonly SyncHolder _sync;
        private ChannelHandler _channel;

        public ObservableCollection<ChatMessage> Chats { get; } = new ObservableCollection<ChatMessage>();

        public void Subscribe(ChannelHandler channel)
        {
            _channel = channel;

            // ChannelHandler chats
            On["/\{channel.MQTTName}/chat"] = _ =>
            {
                LogTo.Trace("\{channel.Name} chat - \{_.Message}");

                if (_sync.sync != null)
                    _sync.sync.Send((x) => Chats.Add(new ChatMessage(_.Message)), null);
                else
                    Chats.Add(new ChatMessage(_.Message));
            };

            Run();
        }

        public ChatProvider(IMQTTSettings settings, SyncHolder sync)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        {
            _sync = sync;
        }
    }
}
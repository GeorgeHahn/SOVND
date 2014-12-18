using System.Collections.ObjectModel;
using Anotar.NLog;
using Charlotte;
using Newtonsoft.Json;
using SOVND.Lib.Models;

// TODO This is client only code, why is it in Lib? (Move to Client)

namespace SOVND.Lib.Handlers
{
    public class ChatProvider : MqttModule, IChatProvider
    {
        public ObservableCollection<ChatMessage> Chats { get; private set; } = new ObservableCollection<ChatMessage>();

        public void ShutdownHandler()
        {
            Disconnect();
            Chats = null;
        }

        public ChatProvider(IMQTTSettings settings, Channel channel)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        {
            // ChannelHandler chats
            On["/" + channel.Name + "/chat"] = _ =>
            {
                LogTo.Trace("[{0}]: {1}", channel.Name, (string)_.Message);

                var msg = JsonConvert.DeserializeObject<ChatMessage>(_.Message);
                Chats.Add(msg);
            };

            Run();
        }
    }
}
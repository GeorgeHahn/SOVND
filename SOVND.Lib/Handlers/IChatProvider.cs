using System.Collections.ObjectModel;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public interface IChatProvider
    {
        ObservableCollection<ChatMessage> Chats { get; }

        //void Subscribe(ChannelHandler channel);

        void ShutdownHandler();
    }
}
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public interface IChatProviderFactory
    {
        ChatProvider CreateChatProvider(Channel channel);
    }
}
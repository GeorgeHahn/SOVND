namespace SOVND.Lib.Handlers
{
    public interface IChannelHandlerFactory
    {
        ChannelHandler CreateChannelHandler(string name);
    }
}
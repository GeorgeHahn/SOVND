namespace SOVND.Client.Modules
{
    public interface IPlayerFactory
    {
        NowPlayingHandler CreatePlayer(string channelName);
    }
}

namespace SOVND.Lib.Handlers
{
    public interface IPlaylistProvider
    {
        bool AddVote(string songID, string username);
        void ClearVotes(string songID);
        int GetVotes(string songID);
        void Subscribe(ChannelHandler channel);
        void ShutdownHandler();
    }
}
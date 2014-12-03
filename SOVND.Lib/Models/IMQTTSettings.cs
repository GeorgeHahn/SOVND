namespace SOVND.Lib.Models
{
    public interface IMQTTSettings
    {
        string Broker { get; }
        int Port { get; }

        string Username { get; }
        string Password { get; }
    }
}

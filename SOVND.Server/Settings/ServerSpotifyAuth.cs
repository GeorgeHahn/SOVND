using System.IO;

namespace SOVND.Server.Settings
{
    public class ServerSpotifyAuth
    {
        public string Username
        {
            get { return File.ReadAllText("spot.username.key"); }
        }

        public string Password
        {
            get { return File.ReadAllText("spot.password.key"); }
        }
    }
}
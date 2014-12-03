using System;
using System.IO;
using System.Threading;
using SOVND.Lib.Settings;
using SpotifyClient;

namespace SOVND.Client.Util
{
    public class StartSpotify
    {
        public StartSpotify(IAppName _appname, ISettingsProvider settings)
        {
            var _auth = settings.GetAuthSettings();

            Spotify.Initialize();
            if (!Spotify.Login(_appname.Name, _auth.SpotifyUsername, _auth.SpotifyPassword))
                throw new Exception("Login failure");

            while (!Spotify.Ready())
                Thread.Sleep(100);
        }
    }
}
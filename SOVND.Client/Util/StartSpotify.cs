using System;
using System.Threading;
using SOVND.Lib.Settings;
using SpotifyClient;

namespace SOVND.Client.Util
{
    public class StartSpotify
    {
        public StartSpotify(IAppName _appname, ISettingsProvider settingsProvider)
        {
            var settings = settingsProvider.GetSettings();

            Spotify.Initialize();
            if (!Spotify.Login(_appname.Name, settings.SpotifyUsername, settings.SpotifyPassword))
                throw new Exception("Login failure");

            while (!Spotify.Ready())
                Thread.Sleep(100);

            Spotify.SetBitrate(settings.Bitrate);

            if(settings.Normalization)
                Spotify.Normalization = settings.Normalization;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anotar.NLog;
using Lpfm.LastFmScrobbler;
using SOVND.Lib.Models;
using SOVND.Lib.Settings;

namespace SOVND.Client.Util
{
    public class Scrobbler
    {
        private readonly SettingsModel _settings;
        private Lpfm.LastFmScrobbler.Scrobbler _scrobbler;
        private Track thisTrack;

        public void Scrobble(Song song, bool playing)
        {
            if (song.track == null || !song.track.Loaded)
                return;

            if (playing)
                thisTrack = new Track
                {
                    TrackName = song.track.Name,
                    AlbumName = song.track.Album.Name,
                    ArtistName = song.track.Artists[0],
                    Duration = TimeSpan.FromSeconds(decimal.ToInt32(song.track.Seconds)),
                    WhenStartedPlaying = DateTime.Now
                };

            if (!_scrobbler.HasSession)
            {
                Auth();
                try
                {
                    if (_settings.LastfmSession == null)
                        _settings.LastfmSession = _scrobbler.GetSession();
                }
                catch (Exception)
                { }
                return;
            }

            try
            {
                if (playing)
                {
                    _scrobbler.NowPlaying(thisTrack);
                }
                else
                {
                    if (thisTrack != null)
                        _scrobbler.Scrobble(thisTrack);
                    thisTrack = null;
                }
            }
            catch (Exception e)
            {
                LogTo.Error("Scrobble error: {0}", e.Message);
            }
        }

        private bool triedtoauth;

        public void Auth()
        {
            if (triedtoauth) return;
            triedtoauth = true;
            Process.Start(_scrobbler.GetAuthorisationUri());
            _settings.LastfmSession = null;
        }

        public Scrobbler(ISettingsProvider settingsProvider)
        {
            _settings = settingsProvider.GetSettings();
            _scrobbler = new Lpfm.LastFmScrobbler.Scrobbler("3f372a470689b8a50d83ce8c40a1a01d", "7e7c030e4b1f6670965ea8d518dcce04", _settings.LastfmSession);
        }
    }
}

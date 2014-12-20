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
            if(_scrobbler.HasSession && string.IsNullOrWhiteSpace(_settings.LastfmSession))
                _settings.LastfmSession = _scrobbler.GetSession();

            if (playing)
            {
                thisTrack = new Track
                {
                    TrackName = song.track.Name,
                    AlbumName = song.track.Album.Name,
                    ArtistName = song.track.Artists[0],
                    Duration = new TimeSpan(0, 0, decimal.ToInt32(song.track.Seconds)),
                    WhenStartedPlaying = new DateTime?(DateTime.Now)
                };
                if (_scrobbler.HasSession)
                    try
                    {
                        _scrobbler.NowPlaying(thisTrack);
                    }
                    catch (Exception e)
                    {
                        LogTo.Error("Scrobble error: {0}", e.Message);
                    }
                return;
            }

            if(thisTrack == null)
                return;
            
            if(_scrobbler.HasSession)
                try
                {
                    _scrobbler.Scrobble(thisTrack);
                }
                catch (Exception e)
                {
                    LogTo.Error("Scrobble error: {0}", e.Message);
                }
            thisTrack = null;
        }

        public Scrobbler(ISettingsProvider settingsProvider)
        {
            _settings = settingsProvider.GetSettings();
                
            _scrobbler = new Lpfm.LastFmScrobbler.Scrobbler("3f372a470689b8a50d83ce8c40a1a01d", "7e7c030e4b1f6670965ea8d518dcce04", _settings.LastfmSession);

            if (string.IsNullOrWhiteSpace(_settings.LastfmSession))
            {
                Process.Start(_scrobbler.GetAuthorisationUri());
            }
        }
    }
}

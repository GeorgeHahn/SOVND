using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace Toastify
{
    public enum SpotifyAction : long
    {
        None = 0,
        ShowToast = 1,
        ShowSpotify = 2,
        CopyTrackInfo = 3,
        SettingsSaved = 4,
        PasteTrackInfo = 5,
        PlayPause = 917504,
        Mute = 524288,
        VolumeDown = 589824,
        VolumeUp = 655360,
        Stop = 851968,
        PreviousTrack = 786432,
        NextTrack = 720896,
        FastForward = 49 << 16,
        Rewind = 50 << 16,
    }

    class Spotify
    {
        private static void ShowSpotify()
        {
            
        }

        public static void SendAction(SpotifyAction a)
        {
            // bah. Because control cannot fall through cases we need to special case volume
            if (SettingsXml.Current.ChangeSpotifyVolumeOnly)
            {
                if (a == SpotifyAction.VolumeUp)
                {
                    VolumeHelper.IncrementVolume("Spotify");
                    return;
                }
                else if (a == SpotifyAction.VolumeDown)
                {
                    VolumeHelper.DecrementVolume("Spotify");
                    return;
                }
                else if (a == SpotifyAction.Mute)
                {
                    VolumeHelper.ToggleApplicationMute("Spotify");
                    return;
                }
            }

            switch (a)
            {
                case SpotifyAction.CopyTrackInfo:
                case SpotifyAction.ShowToast:
                    //Nothing
                    break;
                case SpotifyAction.ShowSpotify:
                    ShowSpotify();
                    break;
            }
        }
    }
}

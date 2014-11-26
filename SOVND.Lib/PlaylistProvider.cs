using Charlotte;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace SOVND.Lib
{
    public interface IPlaylistProvider : INotifyPropertyChanged
    {
        bool AddVote(string songID, string username);
        void ClearVotes(string songID);
        int GetVotes(string songID);

        ObservableCollection<Song> Songs { get; }

        void Subscribe(ChannelHandler channel);
        void Unsubscribe();
    }

    public class PlaylistProvider : MqttModule, IPlaylistProvider
    {
        private ChannelHandler _channel;
        public Action<string> Log = _ => Console.WriteLine(_);

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();
        private readonly ObservableCollection<Song> _songs;

        public bool AddVote(string songID, string username) // TODO: THIS DOES NOT BELONG IN THIS CLASS
        {
            if (!uservotes.ContainsKey(username + songID) || !uservotes[username + songID])
            {
                Log("Vote was valid");

                if (!votes.ContainsKey(songID))
                {
                    votes[songID] = 0;
                }

                votes[songID]++;
                uservotes[username + songID] = true;

                // TODO publish voter names
                return true;
            }
            else
            {
                Log("Vote was invalid");
                return false;
            }
        }

        public void ClearVotes(string songID)
        {
            votes[songID] = 0;

            // TODO this is nasty
            var keystoclear = new List<string>();
            foreach (var key in uservotes.Keys)
            {
                if (key.EndsWith(songID))
                    keystoclear.Add(key);
            }

            foreach(var key in keystoclear)
                uservotes[key] = false;
        }

        public int GetVotes(string songID)
        {
            return votes[songID];
        }

        public ObservableCollection<Song> Songs
        {
            get { return _songs; }
        }

        private void AddNewSong(string ID)
        {
            Log("Added song \{ID}");
            var song = new Song(ID);
            _channel.SongsByID[ID] = song;

            if (SyncHolder.sync != null)
                SyncHolder.sync.Send((x) => _songs.Add(song), null); // TODO Bad bad bad bad
            else
                _songs.Add(song);

            RaisePropertyChanged(nameof(Songs));

            WaitForTrack(song);
        }

        private void WaitForTrack(Song song)
        {
            (new Task(() =>
            {
                if (song.track == null)
                {
                    Thread.Sleep(100);
                    WaitForTrack(song);
                }
                else
                    Log("Song is \{song.track.Name}");
            })).Start();
        }

        public void Subscribe(ChannelHandler channel)
        {
            _channel = channel;

            // ChannelHandler playlists
            On["/\{_channel.MQTTName}/playlist/{songid}/votes"] = _ =>
            {
                Log("\{_channel.Name} got a vote for \{_.songid}");

                if (!channel.SongsByID.ContainsKey(_.songid))
                    AddNewSong(_.songid);
                var song = channel.SongsByID[_.songid];

                song.Votes = int.Parse(_.Message);
            };

            On["/\{_channel.MQTTName}/playlist/{songid}/votetime"] = _ =>
            {
                if (!channel.SongsByID.ContainsKey(_.songid))
                    AddNewSong(_.songid);
                var song = channel.SongsByID[_.songid];
                song.Votetime = long.Parse(_.Message);
            };

            Run();
        }

        public void Unsubscribe()
        {
            Stop();
        }

        public PlaylistProvider(IMQTTSettings settings)
            : base(settings.Broker, settings.Port, settings.Username, settings.Password)
        {
            _songs = new ObservableCollection<Song>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

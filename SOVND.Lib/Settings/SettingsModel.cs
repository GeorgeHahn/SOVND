using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using libspotifydotnet;
using Newtonsoft.Json;

namespace SOVND.Lib.Settings
{
    public class SettingsModel : INotifyPropertyChanged
    {
        public string SOVNDUsername { get; set; }
        public string SOVNDPassword { get; set; }
        public string SpotifyUsername { get; set; }
        public string SpotifyPassword { get; set; }
        public string LastfmUsername { get; set; }
        public string LastfmPassword { get; set; }
        public string LastChannel { get; set; }
        public bool Normalization { get; set; }
        public bool Scrobbling { get; set; }
        public bool SongToasts { get; set; }
        public bool ChatToasts { get; set; }
        public libspotify.sp_bitrate Bitrate { get; set; }

        private string _file;

        // TODO refactor to FilesystemSettingsProvider
        public void Save()
        {
            if (string.IsNullOrEmpty(_file))
                return;

            if (!Directory.Exists(Path.GetDirectoryName(_file)))
                Directory.CreateDirectory(Path.GetDirectoryName(_file));
            File.WriteAllText(_file, JsonConvert.SerializeObject(this));
        }

        private SettingsModel()
        { }

        private SettingsModel(string file)
        {
            _file = file;
        }

        // TODO refactor to FilesystemSettingsProvider
        public static SettingsModel Load(string file)
        {
            SettingsModel settings;
            if (IsSet(file))
            {
                settings = JsonConvert.DeserializeObject<SettingsModel>(File.ReadAllText(file));
                settings._file = file;
            }
            else
                settings = new SettingsModel(file);
            return settings;
        }

        public static bool IsSet(string file)
        {
            return File.Exists(file);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Save();
            if(PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
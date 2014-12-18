using System.IO;
using Newtonsoft.Json;

namespace SOVND.Lib.Settings
{
    public class SettingsModel
    {
        public string SOVNDUsername { get; set; }
        public string SOVNDPassword { get; set; }
        public string SpotifyUsername { get; set; }
        public string SpotifyPassword { get; set; }
        public string LastfmUsername { get; set; }
        public string LastfmPassword { get; set; }
        public string LastChannel { get; set; }
        
        private string _file;

        // TODO refactor to FilesystemSettingsProvider
        public void Save()
        {
            if (!Directory.Exists(Path.GetDirectoryName(_file)))
                Directory.CreateDirectory(Path.GetDirectoryName(_file));
            File.WriteAllText(_file, JsonConvert.SerializeObject(this));
        }

        // TODO refactor to FilesystemSettingsProvider
        public static SettingsModel Load(string file)
        {
            SettingsModel settings;
            if (IsSet(file))
                settings = JsonConvert.DeserializeObject<SettingsModel>(File.ReadAllText(file));
            else
                settings = new SettingsModel();
            settings._file = file;
            return settings;
        }

        public static bool IsSet(string file)
        {
            return File.Exists(file);
        }
    }
}
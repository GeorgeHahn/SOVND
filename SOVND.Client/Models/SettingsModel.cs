using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOVND.Client.ViewModels;
using SOVND.Lib.Settings;

namespace SOVND.Lib.Settings
{
    public interface ISettingsProvider
    {
        SettingsModel GetAuthSettings();
        bool AuthSettingsSet();
    }

    public interface IFileLocationProvider
    {
        string GetRootPath();
        string GetSettingsPath();
        string GetCachePath();
        string GetTempPath();
    }

    public interface IAppName
    {
        string Name { get; }
    }

    public class AppDataLocationProvider : IFileLocationProvider
    {
        private readonly string _appname;

        public AppDataLocationProvider(IAppName appname)
        {
            _appname = appname.Name;
        }

        public string GetRootPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _appname);
        }

        public string GetSettingsPath()
        {
            return Path.Combine(GetRootPath(), "settings", "settings.dat");
        }

        public string GetCachePath()
        {
            return Path.Combine(GetRootPath(), "spotifycache");
        }
        public string GetTempPath()
        {
            return Path.Combine(GetRootPath(), "spotifytemp");
        }
    }

    public class FilesystemSettingsProvider : ISettingsProvider
    {
        private readonly IFileLocationProvider _loc;

        public FilesystemSettingsProvider(IFileLocationProvider loc)
        {
            _loc = loc;
        }

        public SettingsModel GetAuthSettings()
        {
            return SettingsModel.Load(_loc.GetSettingsPath());
        }

        public bool AuthSettingsSet()
        {
            return SettingsModel.IsSet(_loc.GetSettingsPath());
        }
    }

}

namespace SOVND.Client.ViewModels
{
    public class SettingsModel
    {
        public string SOVNDUsername { get; set; }
        public string SOVNDPassword { get; set; }
        public string SpotifyUsername { get; set; }
        public string SpotifyPassword { get; set; }
        public string LastfmUsername { get; set; }
        public string LastfmPassword { get; set; }
        
        [NonSerialized] private string _file;

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

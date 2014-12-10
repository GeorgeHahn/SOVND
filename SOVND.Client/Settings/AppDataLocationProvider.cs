using System.Environment;
using System.IO;
using SOVND.Lib.Settings;

namespace SOVND.Client.Settings
{
    public class AppDataLocationProvider : IFileLocationProvider
    {
        private readonly string _appname;

        public AppDataLocationProvider(IAppName appname)
        {
            _appname = appname.Name;
        }

        public string GetRootPath()
        {
            return Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), _appname);
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
}
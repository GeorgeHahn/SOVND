namespace SOVND.Lib.Settings
{
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
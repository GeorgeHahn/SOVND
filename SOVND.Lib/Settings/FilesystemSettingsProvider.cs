namespace SOVND.Lib.Settings
{
    public class FilesystemSettingsProvider : ISettingsProvider
    {
        private readonly IFileLocationProvider _loc;

        public FilesystemSettingsProvider(IFileLocationProvider loc)
        {
            _loc = loc;
        }

        public SettingsModel GetSettings()
        {
            return SettingsModel.Load(_loc.GetSettingsPath());
        }

        public bool IsSet()
        {
            return SettingsModel.IsSet(_loc.GetSettingsPath());
        }
    }
}
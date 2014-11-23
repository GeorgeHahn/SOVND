namespace SOVND.Lib.Settings
{
    public interface IFileLocationProvider
    {
        string GetRootPath();
        string GetSettingsPath();
        string GetCachePath();
        string GetTempPath();
    }
}
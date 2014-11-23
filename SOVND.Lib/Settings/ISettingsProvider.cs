namespace SOVND.Lib.Settings
{
    public interface ISettingsProvider
    {
        SettingsModel GetAuthSettings();
        bool AuthSettingsSet();
    }
}
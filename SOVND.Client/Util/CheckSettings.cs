using SOVND.Client.ViewModels;
using SOVND.Lib.Settings;

namespace SOVND.Client.Util
{
    public class CheckSettings
    {
        public CheckSettings(ISettingsProvider settings)
        {
            if (!settings.AuthSettingsSet())
            {
                SettingsWindow w = new SettingsWindow();
                var settingsViewModel = new SettingsViewModel(settings.GetAuthSettings());
                w.DataContext = settingsViewModel;
                w.ShowDialog();
            }
        }
    }
}
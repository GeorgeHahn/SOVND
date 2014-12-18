using Anotar.NLog;
using SOVND.Client.ViewModels;
using SOVND.Lib.Settings;

namespace SOVND.Client.Util
{
    public class CheckSettings
    {
        public CheckSettings(ISettingsProvider settings)
        {
            if (!settings.IsSet())
            {
                LogTo.Trace("Auth settings are not set, showing window");
                SettingsWindow w = new SettingsWindow();
                var settingsViewModel = new SettingsViewModel(settings.GetSettings());
                w.DataContext = settingsViewModel;
                w.ShowDialog();
                LogTo.Trace("Auth window closed");
            }
            else
            {
                LogTo.Trace("Auth settings are set");
            }
        }
    }
}
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace SOVND.Lib.Settings
{
    public interface ISettingsProvider
    {
        SettingsModel GetSettings();
        bool IsSet();
    }
}
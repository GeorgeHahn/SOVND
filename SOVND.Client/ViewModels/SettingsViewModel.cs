using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOVND.Lib.Settings;

namespace SOVND.Client.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public SettingsViewModel(SettingsModel settings)
        {
            _settings = settings;
        }

        private SettingsModel _settings;

        public string SOVNDUsername
        {
            get { return _settings.SOVNDUsername; }
            set
            {
                _settings.SOVNDUsername = value;
                _settings.Save();
                if (PropertyChanged != null)
                    RaisePropertyChanged(nameof(SOVNDUsername));
            }
        }

        public string SOVNDPassword
        {
            get { return _settings.SOVNDPassword; }
            set
            {
                _settings.SOVNDPassword = value;
                _settings.Save();
                if (PropertyChanged != null)
                    RaisePropertyChanged(nameof(SOVNDPassword));
            }
        }

        public string SpotifyUsername
        {
            get { return _settings.SpotifyUsername; }
            set
            {
                _settings.SpotifyUsername = value;
                _settings.Save();
                if (PropertyChanged != null)
                    RaisePropertyChanged(nameof(SpotifyUsername));
            }
        }

        public string SpotifyPassword
        {
            get { return _settings.SpotifyPassword; }
            set
            {
                _settings.SpotifyPassword = value;
                _settings.Save();
                if (PropertyChanged != null)
                    RaisePropertyChanged(nameof(SpotifyPassword));
            }
        }

        public string LastfmUsername
        {
            get { return _settings.LastfmUsername; }
            set
            {
                _settings.LastfmUsername = value;
                _settings.Save();
                if (PropertyChanged != null)
                    RaisePropertyChanged(nameof(LastfmUsername));
            }
        }

        public string LastfmPassword
        {
            get { return _settings.LastfmPassword; }
            set
            {
                _settings.LastfmPassword = value;
                _settings.Save();
                if (PropertyChanged != null)
                    RaisePropertyChanged(nameof(LastfmPassword));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

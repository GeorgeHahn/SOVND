using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOVND.Client.ViewModels
{
    public class SettingsViewModel
    {
        public string SOVNDUsername { get; set; }
        public string SOVNDPassword { get; set; }
        public string SpotifyUsername { get; set; }
        public string SpotifyPassword { get; set; }
        public string LastfmUsername { get; set; }
        public string LastfmPassword { get; set; }
    }
}

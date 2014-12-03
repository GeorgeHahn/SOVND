using System.Collections.ObjectModel;
using System.ComponentModel;
using SOVND.Lib.Models;

namespace SOVND.Lib.Handlers
{
    public interface IObservablePlaylistProvider : IPlaylistProvider, INotifyPropertyChanged
    {
        ObservableCollection<Song> Songs { get; }
    }
}
using System.Windows;
using SOVND.Client.Util;
using SOVND.Lib.Models;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for ChannelWindow.xaml
    /// </summary>
    public partial class ChannelWindow : Window
    {
        private readonly ChannelDirectory _channels;

        public ChannelWindow(ChannelDirectory channels)
        {
            _channels = channels;
            InitializeComponent();

            channelbox.ItemsSource = channels.channels;
        }

        public Channel SelectedChannel
        { 
            get { return ((Channel) channelbox.SelectedItem); }
        }
    }
}

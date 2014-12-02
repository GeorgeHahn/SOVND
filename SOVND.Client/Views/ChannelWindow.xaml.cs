using SOVND.Client.Util;
using SOVND.Lib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

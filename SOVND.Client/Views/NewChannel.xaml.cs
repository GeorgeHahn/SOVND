using System.Windows;
using SOVND.Client.ViewModels;

namespace SOVND.Client.Views
{
    /// <summary>
    /// Interaction logic for NewChannel.xaml
    /// </summary>
    public partial class NewChannel : Window
    {
        public string ChannelName { get; private set; }

        public NewChannel(NewChannelViewModel viewmodel)
        {
            InitializeComponent();
            DataContext = viewmodel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (((NewChannelViewModel) DataContext).Register())
            {
                ChannelName = ((NewChannelViewModel) DataContext).Name;
                this.Close();
            }
        }
    }
}

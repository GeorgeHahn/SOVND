using SOVND.Client.ViewModels;
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

namespace SOVND.Client.Views
{
    /// <summary>
    /// Interaction logic for NewChannel.xaml
    /// </summary>
    public partial class NewChannel : Window
    {
        public string ChannelName { get; private set; }

        public NewChannel()
        {
            InitializeComponent();
            DataContext = new NewChannelViewModel();
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

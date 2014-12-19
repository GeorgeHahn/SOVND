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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SOVND.Client.Controls
{
    /// <summary>
    /// Interaction logic for SongControl.xaml
    /// </summary>
    public partial class SongControl : UserControl
    {
        public SongControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SongProperty = DependencyProperty.Register(
            "Song", typeof (Lib.Models.Song), typeof (SongControl), new PropertyMetadata(default(Lib.Models.Song)));

        public Lib.Models.Song Song
        {
            get { return (Lib.Models.Song) GetValue(SongProperty); }
            set { SetValue(SongProperty, value); }
        }

        public event EventHandler OnVoteUp;
        public event EventHandler OnReport;
        public event EventHandler OnDelete;
        public event EventHandler OnBlock;

        private void VoteUp_Click(object sender, RoutedEventArgs e)
        {
            if (OnVoteUp != null)
                OnVoteUp(sender, e);
        }

        private void Rpt_Click(object sender, RoutedEventArgs e)
        {
            if (OnReport != null)
                OnReport(sender, e);
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (OnDelete != null)
                OnDelete(sender, e);
        }

        private void Blk_Click(object sender, RoutedEventArgs e)
        {
            if (OnBlock != null)
                OnBlock(sender, e);
        }
    }
}

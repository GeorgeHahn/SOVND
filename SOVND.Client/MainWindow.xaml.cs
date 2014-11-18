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

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void tbAssembly_Populating(object sender, System.Windows.Controls.PopulatingEventArgs e)
        {
            string text = tbAssembly.Text;
            IntPtr session
            libspotifydotnet.libspotify.sp_search_create()

            if (Directory.Exists(Path.GetDirectoryName(dirname)))
            {
                string[] files = Directory.GetFiles(dirname, "*.*", SearchOption.TopDirectoryOnly);
                string[] dirs = Directory.GetDirectories(dirname, "*.*", SearchOption.TopDirectoryOnly);
                var candidates = new List<string>();

                Array.ForEach(new String[][] { files, dirs }, (x) =>
                    Array.ForEach(x, (y) =>
                    {
                        if (y.StartsWith(dirname, StringComparison.CurrentCultureIgnoreCase))
                            candidates.Add(y);
                    }));

                tbAssembly.ItemsSource = candidates;
                tbAssembly.PopulateComplete();
            }
        }
    }
}

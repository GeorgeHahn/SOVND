using System;
using System.Windows;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IntPtr WindowHandle { get; internal set; }
    }
}

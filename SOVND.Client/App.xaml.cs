using System;
using System.Windows;
using BugSense;
using BugSense.Model;

namespace SOVND.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IntPtr WindowHandle { get; internal set; }

        public App()
        {
            BugSenseHandler.Instance.InitAndStartSession(new ExceptionManager(Current), "w8cb3749"); 
        }
    }
}

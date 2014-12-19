using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Net;
using System.IO;
using System.Xml.XPath;
using System.Xml;
using System.Diagnostics;
using System.Web;
using System.Runtime.InteropServices;
using ServiceStack;
using SOVND.Client.Util;
using SpotifyClient;

namespace Toastify
{
    public partial class Toast : Window
    {
        System.Windows.Forms.NotifyIcon trayIcon;

        internal List<Hotkey> HotKeys { get; set; }
        
        internal static Toast Current { get; private set; }

        private bool dragging = false;

        public void LoadSettings()
        {

            try
            {
                SettingsXml.Current.Load();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception loading settings:\n" + ex);

                MessageBox.Show(@"Toastify was unable to load the settings file." + Environment.NewLine +
                                    "Delete the Toastify.xml file and restart the application to recreate the settings file." + Environment.NewLine +
                                Environment.NewLine +
                                "The application will now be started with default settings.", "Toastify", MessageBoxButton.OK, MessageBoxImage.Information);

                SettingsXml.Current.Default();
            }
        }

        public Toast()
        {
            InitializeComponent();

            // set a static reference back to ourselves, useful for callbacks
            Current = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Load settings from XML
            LoadSettings();

            //Init toast(color settings)
            InitToast();

            //Init tray icon
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Icon = SOVND.Client.Properties.Resources.SOVND;
            trayIcon.Text = "SOVND";
            trayIcon.Visible = true;

            trayIcon.ContextMenu = new System.Windows.Forms.ContextMenu();

            //Init tray icon menu
            System.Windows.Forms.MenuItem menuSettings = new System.Windows.Forms.MenuItem();
            menuSettings.Text = "Settings";
            menuSettings.Click += (s, ev) => { Settings.Launch(this); };

            //trayIcon.ContextMenu.MenuItems.Add(menuSettings);

            //trayIcon.ContextMenu.MenuItems.Add("-");

            System.Windows.Forms.MenuItem menuExit = new System.Windows.Forms.MenuItem();
            menuExit.Text = "Exit";
            menuExit.Click += (s, ev) => { Application.Current.Shutdown(); }; //this.Close(); };

            trayIcon.ContextMenu.MenuItems.Add(menuExit);

            //trayIcon.MouseClick += (s, ev) => { if (ev.Button == System.Windows.Forms.MouseButtons.Left) DisplayAction(SpotifyAction.ShowToast, null); };

            //trayIcon.DoubleClick += (s, ev) => { Settings.Launch(this); };

            this.Deactivated += Toast_Deactivated;

            //Remove from ALT+TAB
            WinHelper.AddToolWindowStyle(this);
        }

        void Toast_Deactivated(object sender, EventArgs e)
        {
            this.Topmost = true;
        }

        public void InitToast()
        {
            const double MIN_WIDTH = 200.0;
            const double MIN_HEIGHT = 65.0;

            //If we find any invalid settings in the xml we skip it and use default.
            //User notification of bad settings will be implemented with the settings dialog.

            //This method is UGLY but we'll keep it until the settings dialog is implemented.
            SettingsXml settings = SettingsXml.Current;

            ToastBorder.BorderThickness = new Thickness(settings.ToastBorderThickness);

            ColorConverter cc = new ColorConverter();
            if (!string.IsNullOrEmpty(settings.ToastBorderColor) && cc.IsValid(settings.ToastBorderColor))
                ToastBorder.BorderBrush = new SolidColorBrush((Color)cc.ConvertFrom(settings.ToastBorderColor));

            if (!string.IsNullOrEmpty(settings.ToastColorTop) && !string.IsNullOrEmpty(settings.ToastColorBottom) && cc.IsValid(settings.ToastColorTop) && cc.IsValid(settings.ToastColorBottom))
            {
                Color top = (Color)cc.ConvertFrom(settings.ToastColorTop);
                Color botton = (Color)cc.ConvertFrom(settings.ToastColorBottom);

                ToastBorder.Background = new LinearGradientBrush(top, botton, 90.0);
            }

            if (settings.ToastWidth >= MIN_WIDTH)
                this.Width = settings.ToastWidth;
            if (settings.ToastHeight >= MIN_HEIGHT)
                this.Height = settings.ToastHeight;

            //If we made it this far we have all the values needed.
            ToastBorder.CornerRadius = new CornerRadius(settings.ToastBorderCornerRadiusTopLeft, settings.ToastBorderCornerRadiusTopRight, settings.ToastBorderCornerRadiusBottomRight, settings.ToastBorderCornerRadiusBottomLeft);
        }

        private Track currentTrack;

        public void NewSong(Track track)
        {
            currentTrack = track;

            this.Dispatcher.Invoke((Action) delegate
            {
                Title1.Text = track.Name;
                Title2.Text = track.AllArtists;
            }, System.Windows.Threading.DispatcherPriority.Normal);

            this.Dispatcher.Invoke((Action) delegate
            {
                FadeIn();
            }, System.Windows.Threading.DispatcherPriority.Normal);

        }

        private void FadeIn(bool force = false, bool isUpdate = false)
        {
            if (dragging)
                return;

            SettingsXml settings = SettingsXml.Current;

            if ((settings.DisableToast || settings.OnlyShowToastOnHotkey) && !force)
                return;

            LogoToast.Source = (BitmapImage)(new System.Windows.Media.ImageConverter().Convert(currentTrack.AlbumArt, null, null, null));

            System.Drawing.Rectangle workingArea = new System.Drawing.Rectangle((int)this.Left, (int)this.Height, (int)this.ActualWidth, (int)this.ActualHeight);
            workingArea = System.Windows.Forms.Screen.GetWorkingArea(workingArea);

            this.Left = settings.PositionLeft;
            this.Top = settings.PositionTop;
            
            ResetPositionIfOffScreen(workingArea);

            DoubleAnimation anim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(250));
            anim.Completed += (s, e) => { FadeOut(); };
            this.BeginAnimation(Window.OpacityProperty, anim);

            this.Topmost = true;
        }

        private void ResetPositionIfOffScreen(System.Drawing.Rectangle workingArea)
        {
            var rect = new System.Drawing.Rectangle((int)this.Left, (int)this.Top, (int)this.Width, (int)this.Height);

            if (!System.Windows.Forms.Screen.AllScreens.Any(s => s.WorkingArea.Contains(rect)))
            {
                // get the defaults, but don't save them (this allows the user to reconnect their screen and get their 
                // desired settings back)
                var position = ScreenHelper.GetDefaultToastPosition(this.Width, this.Height);

                this.Left = position.X;
                this.Top = position.Y;
            }
        }

        private void FadeOut(bool now = false)
        {
            DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(500));
            anim.BeginTime = TimeSpan.FromMilliseconds((now ? 0 : SettingsXml.Current.FadeOutTime));
            this.BeginAnimation(Window.OpacityProperty, anim);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // close Spotify first
            if (SettingsXml.Current.CloseSpotifyWithToastify)
            {
                Process[] possibleSpotifys = Process.GetProcessesByName("Spotify");
                if (possibleSpotifys.Count() > 0)
                {
                    using (Process spotify = possibleSpotifys[0])
                    {

                        try
                        {
                            // try to close spotify gracefully
                            if (spotify.CloseMainWindow())
                            {
                                spotify.WaitForExit(1000);
                            }

                            // didn't work (Spotify often treats window close as hide main window) :( Kill them!
                            if (!spotify.HasExited)
                                Process.GetProcessesByName("Spotify")[0].Kill();
                        }
                        catch { } // ignore all process exceptions
                    }
                }
            }

            // Ensure trayicon is removed on exit. (Thx Linus)
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayIcon = null;

            base.OnClosing(e);
        }

        private System.Windows.Input.Key ConvertKey(System.Windows.Forms.Keys key)
        {
            if (Enum.GetNames(typeof(System.Windows.Input.Key)).Contains(key.ToString()))
                return (System.Windows.Input.Key)Enum.Parse(typeof(System.Windows.Input.Key), key.ToString());
            else
                return Key.None;
        }

        #region ActionHookCallback

        private static Hotkey _lastHotkey = null;
        private static DateTime _lastHotkeyPressTime = DateTime.Now;

        /// <summary>
        /// If the same hotkey press happens within this buffer time, it will be ignored.
        /// 
        /// I came to 150 by pressing keys as quickly as possibly. The minimum time was less than 150
        /// but most values fell in the 150 to 200 range for quick presses, so 150 seemed the most reasonable
        /// </summary>
        private const int WAIT_BETWEEN_HOTKEY_PRESS = 150;

        internal static void ActionHookCallback(Hotkey hotkey)
        {
        }

        private static void SendPasteKey(Hotkey hotkey)
        {
            var shiftKey = new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.ShiftKey);
            var altKey   = new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.Alt);
            var ctrlKey  = new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.ControlKey);
            var vKey     = new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.V);

            // Before injecting a paste command, first make sure that no modifiers are already
            // being pressed (which will throw off the Ctrl+v).
            // Since key state is notoriously unreliable, set a max sleep so that we don't get stuck
            var maxSleep = 250;

            // minimum sleep time
            System.Threading.Thread.Sleep(150);

            //System.Diagnostics.Debug.WriteLine("shift: " + shiftKey.State + " alt: " + altKey.State + " ctrl: " + ctrlKey.State);

            while (maxSleep > 0 && (shiftKey.State != 0 || altKey.State != 0 || ctrlKey.State != 0))
                System.Threading.Thread.Sleep(maxSleep -= 50);

            //System.Diagnostics.Debug.WriteLine("maxSleep: " + maxSleep);

            // press keys in sequence. Don't use PressAndRelease since that seems to be too fast
            // for most applications and the sequence gets lost.
            ctrlKey.Press();
            vKey.Press();
            System.Threading.Thread.Sleep(25);
            vKey.Release();
            System.Threading.Thread.Sleep(25);
            ctrlKey.Release();
        }

        private static void CopySongToClipboard(string trackBeforeAction)
        {
            var template = SettingsXml.Current.ClipboardTemplate;

            // if the string is empty we set it to {0}
            if (string.IsNullOrWhiteSpace(template))
                template = "{0}";

            // add the song name to the end of the template if the user forgot to put in the
            // replacement marker
            if (!template.Contains("{0}"))
                template += " {0}";

            Clipboard.SetText(string.Format(template, trackBeforeAction));
        }

        #endregion

        public void DisplayAction(SpotifyAction action, string trackBeforeAction)
        {
            //Anything that changes track doesn't need to be handled since
            //that will be handled in the timer event.

            const string VOLUME_UP_TEXT = "Volume ++";
            const string VOLUME_DOWN_TEXT = "Volume --";
            const string MUTE_ON_OFF_TEXT = "Mute On/Off";
            const string NOTHINGS_PLAYING = "Nothing's playing";
            const string PAUSED_TEXT = "Paused";
            const string STOPPED_TEXT = "Stopped";
            const string SETTINGS_TEXT = "Settings saved";

            string currentTitle = currentTrack.Name;

            string prevTitle1 = Title1.Text;
            string prevTitle2 = Title2.Text;

            switch (action)
            {
                case SpotifyAction.PlayPause:
                    if (!string.IsNullOrEmpty(trackBeforeAction))
                    {
                        //We pressed pause
                        Title1.Text = "Paused";
                        Title2.Text = trackBeforeAction;
                        FadeIn();
                    }
                    break;
                case SpotifyAction.Stop:
                    Title1.Text = "Stopped";
                    Title2.Text = trackBeforeAction;
                    FadeIn();
                    break;
                case SpotifyAction.SettingsSaved:
                    Title1.Text = SETTINGS_TEXT;
                    Title2.Text = "Here is a preview of your settings!";
                    FadeIn();
                    break;
                case SpotifyAction.NextTrack:      //No need to handle
                    break;
                case SpotifyAction.PreviousTrack:  //No need to handle
                    break;
                case SpotifyAction.VolumeUp:
                    Title1.Text = VOLUME_UP_TEXT;
                    Title2.Text = currentTitle;
                    FadeIn();
                    break;
                case SpotifyAction.VolumeDown:
                    Title1.Text = VOLUME_DOWN_TEXT;
                    Title2.Text = currentTitle;
                    FadeIn();
                    break;
                case SpotifyAction.Mute:
                    Title1.Text = MUTE_ON_OFF_TEXT;
                    Title2.Text = currentTitle;
                    FadeIn();
                    break;
                case SpotifyAction.ShowToast:
                    //if (string.IsNullOrEmpty(currentTrack) && Title1.Text != PAUSED_TEXT && Title1.Text != STOPPED_TEXT)
                    //{
                    //    coverUrl = "SpotifyToastifyLogo.png";

                    //    if (SettingsXml.Current.UseSpotifyBeta)
                    //    {
                    //        Title1.Text = "No Spotify Beta Support :(";
                    //        Title2.Text = "Hotkeys will still work";
                    //    }
                    //    else
                    //    {
                    //        Title1.Text = NOTHINGS_PLAYING;
                    //        Title2.Text = string.Empty;
                    //    }
                    //}
                    //else
                    //{
                    //    string part1, part2;
                    //    if (SplitTitle(currentTrack, out part1, out part2))
                    //    {
                    //        Title1.Text = part2;
                    //        Title2.Text = part1;
                    //    }
                    //}
                    FadeIn(force: true);
                    break;
                case SpotifyAction.ShowSpotify:  //No need to handle
                    break;
            }
        }

        /// <summary>
        /// Mouse is over the window, halt any fade out animations and keep
        /// the toast active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            this.BeginAnimation(Window.OpacityProperty, null);
            this.Opacity = 1.0;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            FadeOut();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                dragging = true;
                DragMove();
                return;
            }

            FadeOut(now: true);

            Spotify.SendAction(SpotifyAction.ShowSpotify);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging)
            {
                dragging = false;

                // save the new window position
                SettingsXml settings = SettingsXml.Current;

                settings.PositionLeft = this.Left;
                settings.PositionTop  = this.Top;

                settings.Save();
            }
        }

        
    }
}

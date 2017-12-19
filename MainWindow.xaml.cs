using EventRiteComposer.Data;
using EventRiteComposer.NAudio.AudioPlayback;
using EventRiteComposer.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Utilities;

namespace EventRiteComposer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow instance;
        public static IniFile iniFile;
        public static event EventHandler OnApplicationQuit = (object sender, EventArgs e) => { };
        public static event EventHandler<VolumeValueChangedEventArgs> OnVolumeValueChanged = (object sender, VolumeValueChangedEventArgs e) => { };
        public static event EventHandler<VolumeValueChangedEventArgs> OnSeekPositionChanged = (object sender, VolumeValueChangedEventArgs e) => { };
        public static event EventHandler<KeyEventArgs> OnWindowKeyDown = (object sender, System.Windows.Input.KeyEventArgs e) => { };
        public static event EventHandler<RoutedEventArgs> OnButtonStopPressed = (object sender, RoutedEventArgs e) => { };
        public static ProgressDataProvider m_ProgressDataProvider;
        public static ProgressDataProvider ProgressDataProvider
        {
            set
            {
                m_ProgressDataProvider = value;
                if(m_ProgressDataProvider is AudioProgressDataProvider)
                    instance.waveformTimeline.RegisterSoundPlayer(((AudioProgressDataProvider)m_ProgressDataProvider).AudioPlayer);
            }
            get { return m_ProgressDataProvider; }
        }
        private DispatcherTimer dispatcherTimer;
        string m_IniPath = System.IO.Path.Combine(Environment.CurrentDirectory, "settings.ini");

        string MPC = @"MPC\mpc-hc.exe";
        string vlc = @"vlc\vlc.exe";

        string MusicBee = @"MusicBee\MusicBee.exe";
        string Apollo = @"Apollo\Apollo.exe";
        string winamp = @"winamp\winamp.exe";
        string Clementine = @"Clementine\Clementine.exe";
        string AIMP3 = @"AIMP3\aimp3.exe";
        string AIMP4 = @"AIMP4\aimp.exe";
        string AIMP2 = @"AIMP2\aimp2.exe";
        string foobar2000 = @"foobar2000\foobar2000.exe";

        private PlaybackInfo audioPlayer;
        private PlaybackInfo videoPlayer;
        private Preferences m_Preferences;
        private Point startLocation;
        private Point lastLocation;
        private bool isDragging;

        public Preferences Preferences
        {
            get { return m_Preferences; }
            set { m_Preferences = value; }
        }

        public MainWindow()
        {
            instance = this;

            InitializeComponent();

            Initialize();
        }

        private void Initialize()
        {
            IniFileInit();
            m_Preferences = new Preferences(iniFile);

            var asName = typeof(MainWindow).Assembly.GetName();
            Title = String.Format("{0} v{1}b", asName.Name, asName.Version);

            PlaybackStage.OnPlay += PlaybackStage_OnPlay;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 150);
            dispatcherTimer.Start();
        }

        private void PlaybackStage_OnPlay(PlaybackStage sender, PlaybackStageEventArgs e)
        {
            if (String.IsNullOrEmpty(sender.PlaybackInfo.MediaFilePath) || !System.IO.File.Exists(sender.PlaybackInfo.MediaFilePath))
                return;

            TextBlockTrackTitle.Text = string.Format("{0}", System.IO.Path.GetFileName(sender.PlaybackInfo.MediaFilePath));
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (ProgressDataProvider != null)
            {
                TimeSpan currentTime = ProgressDataProvider.CurrentTime;
                TimeSpan totalTime = ProgressDataProvider.TotalTime;

                if (!sliderSeekdragStarted)
                    SliderSeek.Value = ProgressDataProvider.Progress;
                ProgressBarPosition.Value = SliderSeek.Value;

                TextBlockPlayedTime.Text = String.Format("{0:00}:{1:00}", (int)currentTime.TotalMinutes, currentTime.Seconds);
                TextBlockTotalTime.Text = String.Format("{0:00}:{1:00}", (int)totalTime.TotalMinutes, totalTime.Seconds);
            }
            else
            {
                SliderSeek.Value = ProgressBarPosition.Value = 0;
            }
        }

        void IniFileInit()
        {
            if (!File.Exists(m_IniPath))
            {
                var fstreem = File.Create(m_IniPath);
                fstreem.Close();

                iniFile = new IniFile(m_IniPath);
                SaveSettings();
            }
            else
            {
                iniFile = new IniFile(m_IniPath);
            }
        }

        void SaveSettings()
        {
            try
            {
                m_Preferences.Save();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка сохранения настроек. mainwindow" + ex.Message, "Ошибка");
            }
        }

        private bool m_IsDragInProgress { get; set; }
        private System.Windows.Point m_FormMousePosition { get; set; }

        #region DllImport
        private const int L_KEY = 0x4C;
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int WM_APPCOMMAND = 0x319;
        private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 0xE0000;
        private const int APPCOMMAND_MEDIA_STOP = 0xD0000;
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(int ZeroOnly, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        #endregion

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Diagnostics.Debug.Print(e.Key.ToString());
            OnWindowKeyDown(sender, e);
            var isCtrl = (System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl));
            if (e.Key == Key.O && isCtrl)
            {
                var wnd = new OptionsWindow(new List<IOutputDevicePlugin>() {
                    new WasapiOutPlugin(),
                    new WaveOutPlugin(),
                    new DirectSoundOutPlugin(),
                    new NullOutPlugin()
                });

                wnd.ShowDialog();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            OnApplicationQuit(this, EventArgs.Empty);
            SaveSettings();
        }

        #region MyRegion
        public Point MousePosition
        {
            set
            {
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)value.X, (int)value.Y);
            }
            get { return new Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y); }
        }
        public Point Location
        {
            set
            {
                var location = PointToScreen(value);
                this.Left = value.X + this.Width / 2;
                this.Top = value.Y + this.Height / 2;
            }
            get { return new Point(this.Left, this.Top - this.Height); }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseButtonEventArgs mouse = (MouseButtonEventArgs)e;
            var mouse2 = MousePosition;
            if (mouse.ChangedButton == MouseButton.Left)
            {
                isDragging = true;
                lastLocation = MousePosition;// mouse.GetPosition(null);
                startLocation = new Point(lastLocation.X, lastLocation.Y);
            }
        }
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseButtonEventArgs mouse = (MouseButtonEventArgs)e;

            if (mouse.ChangedButton == MouseButton.Left)
            {
                isDragging = false;

            }
        }
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isDragging)
                return;

            var mouseLocation = MousePosition;
            var newLocation = new Point(mouseLocation.X - startLocation.X, mouseLocation.Y - startLocation.Y);

            Location = newLocation;

        }
        #endregion

        #region Window Drag
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!Properties.Settings.Default.WindowDrag)
            {
                base.OnMouseDown(e);
                return;
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                this.CaptureMouse();
                this.m_IsDragInProgress = true;
                // 
                this.m_FormMousePosition = e.GetPosition((UIElement)this);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!Properties.Settings.Default.WindowDrag)
            {
                base.OnMouseMove(e);
                return;
            }
            if (!this.m_IsDragInProgress)
                return;

            System.Drawing.Point screenPos = (System.Drawing.Point)System.Windows.Forms.Cursor.Position;
            double top = (double)screenPos.Y - (double)this.m_FormMousePosition.Y;
            double left = (double)screenPos.X - (double)this.m_FormMousePosition.X;
            this.SetValue(MainWindow.TopProperty, top);
            this.SetValue(MainWindow.LeftProperty, left);

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (!Properties.Settings.Default.WindowDrag)
            {
                base.OnMouseUp(e);
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                this.m_IsDragInProgress = false;
                this.ReleaseMouseCapture();
            }
            base.OnMouseUp(e);
        }

        #endregion


        private DateTime sliderSeekMouseDownStart;
        private bool sliderSeekdragStarted;

        private void SliderSeek_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            //if (waveOut != null)
            //{
            //    audioFileReader.CurrentTime = TimeSpan.FromSeconds(audioFileReader.TotalTime.TotalSeconds * SliderSeek.Value / 100.0);
            //}

            //sliderSeekdragStarted = false;
        }

        private void SliderSeek_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            if (e.ChangedButton != MouseButton.Left)
                return;

            //if (waveOut == null) return;

            if (DateTime.Now - sliderSeekMouseDownStart > TimeSpan.FromMilliseconds(300))
                SliderSeek_ThumbDragCompleted(null, null);
            else
            {

                var pos = e.GetPosition(SliderSeek);

                SliderSeek.Value = SmoothStep(pos.X, 0, SliderSeek.ActualWidth, SliderSeek.Minimum, SliderSeek.Maximum);

                OnSeekPositionChanged(this, new VolumeValueChangedEventArgs(SliderSeek.Value));
                //    audioFileReader.CurrentTime = TimeSpan.FromSeconds(audioFileReader.TotalTime.TotalSeconds * SliderSeek.Value / 100.0);

                sliderSeekdragStarted = false;
            }
        }

        private void SliderSeek_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            sliderSeekMouseDownStart = DateTime.Now;
            sliderSeekdragStarted = true;
        }

        private void SliderSeek_ThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            sliderSeekdragStarted = true;
        }

        private void SliderSeek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if (waveOut != null && sliderSeekdragStarted)
            //{
            //    audioFileReader.CurrentTime = TimeSpan.FromSeconds(audioFileReader.TotalTime.TotalSeconds * SliderSeek.Value / 100.0);
            //}
            if (ProgressBarPosition != null)
                ProgressBarPosition.Value = SliderSeek.Value;
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if (InvokeSetVolumeDelegate != null)
            //{
            //    InvokeSetVolumeDelegate((float)SliderVolume.Value);
            //}

            if (ProgressBarVolume != null)
            {
                ProgressBarVolume.Value = SliderVolume.Value;

                OnVolumeValueChanged(this, new VolumeValueChangedEventArgs(ProgressBarVolume.Value));
            }
        }

        private void ButtonPlayCommand_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonPauseCommand_Click(object sender, RoutedEventArgs e)
        {
            OnButtonStopPressed(sender, e);
        }



        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "Do you want to quit?";
            string caption = "Event Composer";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;


            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    // User pressed Yes button
                    Close();
                    break;
                case MessageBoxResult.No:
                    // User pressed No button
                    // ...
                    break;
                case MessageBoxResult.Cancel:
                    // User pressed Cancel button
                    // ...
                    break;
            }
        }

        private void OpenPlayers_Click(object sender, RoutedEventArgs e)
        {
            DirectoryCopy(
                Path.Combine(Environment.CurrentDirectory, @"AppData\Roaming"),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), true);

            isInitialized = true;
        }
        public static double SmoothStep(double x, double a1, double a2, double c1, double c2)
        {
            return c1 + ((x - a1) / (a2 - a1)) * (c2 - c1) / 1.0f;
        }
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public static bool isInitialized { get; set; }

        private void GridStack1_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}

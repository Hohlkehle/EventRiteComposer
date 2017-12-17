using EventRiteComposer.Core;
using EventRiteComposer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace EventRiteComposer
{
    /// <summary>
    /// Interaction logic for PlaybackStage.xaml
    /// </summary>
    public partial class PlaybackStage : UserControl
    {
        public enum PlaybackState
        {
            Stoped,
            Playing,
            Paused
        }

        public enum StageType
        {
            None = 0x0,
            Audio = 0x1,
            Video = 0x2
        }

        public delegate void PlaybackStageEventHandler(PlaybackStage sender, PlaybackStageEventArgs e);
        public static event PlaybackStageEventHandler OnPlay, OnVideoPropertyChanged, OnStateChanged;
        static string[] audioExtensions = { ".WAV", ".MID", ".MIDI", ".WMA", ".MP3", ".OGG", ".RMA" };
        static string[] videoExtensions = { ".AVI", ".MP4", ".DIVX", ".WMV" };
        static string[] imageExtensions = { ".PNG", ".JPG", ".JPEG", ".BMP", ".GIF" };

        StageType m_StageType;
        public PlaybackInfo audioPlayer { protected set; get; }
        public PlaybackInfo videoPlayer { protected set; get; }
        PlaybackState m_PlaybackState = PlaybackState.Stoped;
        LinearGradientBrush gradientBrush, gradientHoverBrush, gradientActiveBrush;
        private PlaybackManager m_PlaybackManager;

        MainWindow mainWindow { get { return MainWindow.instance; } }
        string audioFile, videoFile;

        public int StackId { set; get; }
        public int IsSet { set; get; }
        public bool IsStopOthers { get { return CheckBoxStopOther.IsChecked == true ? true : false; } }
        public string KeyName { set; get; }

        public StageType stageType
        {
            get { return m_StageType; }
            set
            {
                m_StageType = value;
                if (m_StageType == StageType.Audio)
                {
                    audioPlayer = PlaybackInfo;//new PlaybackInfo(mainWindow.Preferences.AudioPlayer.Value) { StackId = StackId };
                                               //audioPlayer.StageType = StageType.Audio;


                    TextBlockAudioFileName.Text = System.IO.Path.GetFileNameWithoutExtension(PlaybackInfo.MediaFilePath);

                    ImageAudio.Opacity = 100d;
                    ImageVideo.Opacity = 0d;
                }
                else if (m_StageType == StageType.Video)
                {
                    videoPlayer = PlaybackInfo;// new VlcPlayer(mainWindow.Preferences.VideoPlayer.Value) { StackId = StackId };
                    //videoPlayer.StageType = StageType.Video;

                    TextBlockAudioFileName.Text = System.IO.Path.GetFileNameWithoutExtension(PlaybackInfo.MediaFilePath);


                    ImageVideo.Opacity = 100d;
                    ImageAudio.Opacity = 0d;
                }
                else
                {
                    TextBlockAudioFileName.Text = "Error: Unsuported format!";
                    ImageAudio.Opacity = 0d;
                    ImageVideo.Opacity = 0d;
                }
            }
        }

        public PlaybackState playbackState
        {
            get { return m_PlaybackState; }
            set
            {
                var prev = m_PlaybackState;

                m_PlaybackState = value;

                if (OnStateChanged != null)
                    OnStateChanged(this, new PlaybackStageEventArgs(prev));
            }
        }
        private PlaybackInfo m_PlaybackInfo;
        public PlaybackInfo PlaybackInfo
        {
            get
            {
                return m_PlaybackInfo;
            }
            set
            {
                m_PlaybackInfo = value;
                stageType = m_PlaybackInfo.StageType;
                if (m_PlaybackInfo.StageType == StageType.Audio)
                {
                    audioPlayer = m_PlaybackInfo;
                }

                PlaybackManager = new PlaybackManager(PlaybackInfo);
            }
        }

        public PlaybackManager PlaybackManager { get => m_PlaybackManager; protected set => m_PlaybackManager = value; }

        public PlaybackStage()
        {
            InitializeComponent();
            InitializeCustom();
        }

        internal void InitializePlaybackInfo(int stackId)
        {
            PlaybackInfo = new PlaybackInfo(stackId);
        }

        private void InitializeCustom()
        {
            MainWindow.OnWindowKeyDown += (object sender, System.Windows.Input.KeyEventArgs e) =>
            {
                ProcessKeyPress(e.Key.ToString());
            };

            MainWindow.OnVolumeValueChanged += (object sender, VolumeValueChangedEventArgs e) =>
            {
                PlaybackManager.SetVolume(e.Value);
            };

            MainWindow.OnSeekPositionChanged += (object sender, VolumeValueChangedEventArgs e) =>
            {
                PlaybackManager.SeekToTime(e.Value);
            };

            OnPlay += (PlaybackStage sender, PlaybackStageEventArgs e) =>
            {
                if (sender != this && sender.IsStopOthers && PlaybackManager.IsPlaying)
                    Stop();
            };

            /*if (mainWindow != null)
            {
                mainWindow.KeyDown += mainWindow_KeyDown;
                mainWindow.MenuItemCloseAll.Click += MenuItemCloseAll_Click;
                mainWindow.MenuItemCloseAudio.Click += MenuItemCloseAudio_Click;
                mainWindow.MenuItemCloseVideo.Click += MenuItemCloseVideo_Click;
                mainWindow.MenuItemSave.Click += MenuItemSave_Click;
                Task.Factory.StartNew(new Action(() =>
                {
                    Thread.Sleep(2500);
                    mainWindow.ClosePlayers.Click += MenuItemCloseAll_Click;
                    mainWindow.OpenPlayers.Click += OpenPlayers_Click;
                }));
            }*/

            gradientBrush = ((LinearGradientBrush)Border.Background).Clone();
            gradientHoverBrush = new LinearGradientBrush(Color.FromRgb(100, 90, 190), Color.FromRgb(60, 130, 210), new Point(0.5, 0), new Point(0.5, 1));
            gradientActiveBrush = new LinearGradientBrush(Color.FromRgb(170, 130, 220), Color.FromRgb(90, 170, 230), new Point(0.5, 1), new Point(0.5, 0));


            OnStateChanged += PlaybackStage_OnStateChanged;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;

        }

        void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            audioPlayer.SaveSettings(StackId, typeof(PlaybackInfo));
            videoPlayer.SaveSettings(StackId, typeof(VlcPlayer));
        }

        void OpenPlayers_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                while (!MainWindow.isInitialized)
                {
                    if (audioPlayer != null && audioPlayer.Process == null || audioPlayer.Process.HasExited)
                    {
                        audioPlayer.StartPlayer();
                    }

                    if (videoPlayer != null && videoPlayer.Process == null || videoPlayer.Process.HasExited)
                    {
                        videoPlayer.StartPlayer();
                    }
                }
            });
        }

        void MenuItemCloseVideo_Click(object sender, RoutedEventArgs e)
        {
            if (videoPlayer != null && videoPlayer.Process != null && !videoPlayer.Process.HasExited)
                videoPlayer.Terminate();
        }

        void MenuItemCloseAudio_Click(object sender, RoutedEventArgs e)
        {
            if (audioPlayer != null && audioPlayer.Process != null && !audioPlayer.Process.HasExited)
                audioPlayer.Terminate();
        }

        void MenuItemCloseAll_Click(object sender, RoutedEventArgs e)
        {
            if (audioPlayer != null && audioPlayer.Process != null && !audioPlayer.Process.HasExited)
                audioPlayer.Terminate();
            if (videoPlayer != null && videoPlayer.Process != null && !videoPlayer.Process.HasExited)
                videoPlayer.Terminate();
        }

        protected override void ParentLayoutInvalidated(UIElement child)
        {
            base.ParentLayoutInvalidated(child);
            OrderId.Text = StackId.ToString();
        }

        void LoadFileToStack(string file)
        {
            if (IsAudioFile(file))
            {
                TextBlockAudioFileName.Text = System.IO.Path.GetFileNameWithoutExtension(file);
                stageType = PlaybackStage.StageType.Audio;
                PlaybackInfo.StageType = StageType.Audio;
                audioPlayer.MediaFilePath = audioFile = file;
                ImageAudio.Opacity = 100d;
                ImageVideo.Opacity = 0d;
            }
            else if (IsVideoFile(file))
            {
                TextBlockAudioFileName.Text = System.IO.Path.GetFileNameWithoutExtension(file);
                stageType = PlaybackStage.StageType.Video;
                PlaybackInfo.StageType = StageType.Video;
                videoPlayer.MediaFilePath = videoFile = file;
                ImageVideo.Opacity = 100d;
                ImageAudio.Opacity = 0d;
            }
            else
            {
                TextBlockAudioFileName.Text = "Error: Unsuported format!";
                PlaybackInfo.StageType = StageType.None;
                ImageAudio.Opacity = 0d;
                ImageVideo.Opacity = 0d;
            }

            //InputAudioFile.Text = fileList[0];
            // For example add all files into a simple label control:
            //foreach (string File in FileList)
            //    this.DropLocationLabel.Content += File + "\n";
        }



        public void Play()
        {
            if (stageType == StageType.None)
                return;
            //if (stageType == StageType.Audio)
            //    audioPlayer.Play(audioFile);

            //if (stageType == StageType.Video)
            //    videoPlayer.Play(videoFile);

            if (PlaybackManager.IsPlaying)
            {
                PlaybackManager.Pause();
                Border.Background = gradientHoverBrush;
            }
            else
            {
                PlaybackManager.Play();
                Border.Background = gradientActiveBrush;

                OnPlay?.Invoke(this, new PlaybackStageEventArgs(PlaybackState.Paused));
            }

            Border.Background = gradientActiveBrush;

            playbackState = PlaybackState.Playing;
        }

        public void Stop()
        {
            PlaybackManager?.Stop();

            //if (stageType == StageType.Audio)
            //    audioPlayer.Stop();

            //if (stageType == StageType.Video)
            //    videoPlayer.Stop();


            Border.Background = gradientBrush;

            playbackState = PlaybackState.Stoped;

            ///System.Windows.SystemColors.MenuHighlightBrush;
        }

        public bool IsAudioFile(string path)
        {
            return audioExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        public bool IsVideoFile(string path)
        {
            return videoExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        public bool IsImageFile(string path)
        {
            return imageExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        void PlaybackStage_OnStateChanged(PlaybackStage sender, PlaybackStageEventArgs e)
        {
            if (sender == this)
            { }
            else if (playbackState == PlaybackState.Playing && sender.playbackState == PlaybackState.Playing && sender.CheckBoxStopOther.IsChecked.Value == true)
            {
                Stop();
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Object item = (object)e.Data.GetData(DataFormats.FileDrop);

                // Perform drag-and-drop, depending upon the effect.
                if (((e.Effects & DragDropEffects.Copy) == DragDropEffects.Copy) ||
                   ((e.Effects & DragDropEffects.Move) == DragDropEffects.Move))
                {

                    // Extract the data from the DataObject-Container into a string list
                    string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                    LoadFileToStack(fileList[0]);
                }
            }
            else
            {
                // Reset the label text.
                //DropLocationLabel.Content = "None";
            }
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                foreach (string file in FileList)
                {
                    if (IsAudioFile(file) || IsVideoFile(file))
                    {
                        Border.BorderBrush.Opacity = 100d;
                    }
                    else
                    {
                        Border.BorderBrush.Opacity = 10d;
                    }
                }
            }

            // Check if the Dataformat of the data can be accepted
            // (we only accept file drops from Explorer, etc.)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy; // Okay
            else
                e.Effects = DragDropEffects.None; // Unknown data, ignore it
        }

        void mainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            ProcessKeyPress(e.Key.ToString());
        }

        void ProcessKeyPress(string key)
        {
            if (key == KeyName)
            {
                Play();

                //switch (playbackState)
                //{
                //    case PlaybackState.Stoped:
                //        Play();
                //        break;
                //    case PlaybackState.Playing:
                //        Stop();
                //        break;
                //    case PlaybackState.Paused:
                //        Play();
                //        break;
                //    default:
                //        break;
                //}
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        { }

        void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            if (mainWindow != null)
                mainWindow.KeyDown -= mainWindow_KeyDown;

            //PlaybackInfo.SaveSettings(audioPlayer, mainWindow.Preferences.ConfigPath.Value + "stack" + StackId + "audio.xml");
            //PlaybackInfo.SaveSettings(videoPlayer, mainWindow.Preferences.ConfigPath.Value + "stack" + StackId + "video.xml");
            //if (audioPlayer != null)
            //    audioPlayer.SaveSettings(StackId, typeof(PlaybackInfo));
            //if (videoPlayer != null)
            //    videoPlayer.SaveSettings(StackId, typeof(VlcPlayer));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
                return;

            try
            {
                //audioPlayer = PlaybackInfo.LoadSettings(mainWindow.Preferences.ConfigPath.Value + "stack" + StackId + "audio.xml", typeof(PlaybackInfo));
                //videoPlayer = PlaybackInfo.LoadSettings(mainWindow.Preferences.ConfigPath.Value + "stack" + StackId + "video.xml", typeof(VlcPlayer));

                //audioPlayer = PlaybackInfo.Load(StackId, typeof(PlaybackInfo));
                //videoPlayer = PlaybackInfo.Load(StackId, typeof(VlcPlayer));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PlaybackStage");
            }

            if (audioPlayer.StageType == StageType.Audio && audioPlayer != null && !String.IsNullOrEmpty(audioPlayer.MediaFilePath))
                LoadFileToStack(audioPlayer.MediaFilePath);

            if (videoPlayer.StageType == StageType.Video && videoPlayer != null && !String.IsNullOrEmpty(videoPlayer.MediaFilePath))
                LoadFileToStack(videoPlayer.MediaFilePath);
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Border.Background = gradientHoverBrush;

            //switch (playbackState)
            //{
            //    case PlaybackState.Stoped:
            //        Border.Background = gradientHoverBrush;
            //        break;
            //    case PlaybackState.Playing:
            //        Border.Background = gradientActiveBrush;
            //        break;
            //    case PlaybackState.Paused:
            //        Border.Background = gradientHoverBrush;
            //        break;
            //    default:
            //        break;
            //}
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (PlaybackManager.IsPlaying)
            {
                Border.Background = gradientActiveBrush;
            }
            else
            {
                Border.Background = gradientBrush;
            }
            //switch (playbackState)
            //{
            //    case PlaybackState.Stoped:
            //        Border.Background = gradientBrush;
            //        break;
            //    case PlaybackState.Playing:
            //        Border.Background = gradientActiveBrush;
            //        break;
            //    case PlaybackState.Paused:
            //        Border.Background = gradientBrush;
            //        break;
            //    default:
            //        break;
            //}
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var s = StackId;
            if (e.ChangedButton == MouseButton.Left)
            {

                ProcessKeyPress(KeyName);
            }
        }

        public static PlaybackInfo ToPlaybackInfo(PlaybackStage stage)
        {
            return (PlaybackInfo)stage.PlaybackInfo;
        }
    }
}

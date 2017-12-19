using EventRiteComposer.Core;
using EventRiteComposer.Data;
using EventRiteComposer.WPFSoundVisualizationLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
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

        private StageType m_StageType;
        public PlaybackInfo audioPlayer { protected set; get; }
        public PlaybackInfo videoPlayer { protected set; get; }
        private PlaybackState m_PlaybackState = PlaybackState.Stoped;
        private LinearGradientBrush m_GradientBrush, m_GradientHoverBrush, m_GradientActiveBrush, m_GradientInactiveBrush;
        private PlaybackManager m_PlaybackManager;

        MainWindow mainWindow { get { return MainWindow.instance; } }
        string audioFile, videoFile;

        public int StackId { set; get; }
        public int IsSet { set; get; }
        public bool IsStopOthers { get { return CheckBoxStopOther.IsChecked.Value; } }
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

                }
                else if (m_StageType == StageType.Video)
                {
                    videoPlayer = PlaybackInfo;// new VlcPlayer(mainWindow.Preferences.VideoPlayer.Value) { StackId = StackId };
                    //videoPlayer.StageType = StageType.Video;

                    TextBlockAudioFileName.Text = System.IO.Path.GetFileNameWithoutExtension(PlaybackInfo.MediaFilePath);
                }
                else
                {
                    TextBlockAudioFileName.Text = "Error: Unsuported format!";
                }

                InvalidateStageState();
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
                stageType = (System.IO.File.Exists(PlaybackInfo.MediaFilePath)) ? m_PlaybackInfo.StageType : StageType.None;

                PlaybackManager = new PlaybackManager(PlaybackInfo, this);
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
                

                if (sender != this && sender.IsStopOthers && IsStopOthers && PlaybackManager.IsPlaying)
                    Stop();
                else if (sender != this && (sender.IsStopOthers || sender.PlaybackInfo.StageType == StageType.Video) && PlaybackInfo.StageType == StageType.Video && PlaybackManager.IsPlaying)
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

            m_GradientBrush = ((LinearGradientBrush)Border.Background).Clone();
            m_GradientHoverBrush = new LinearGradientBrush(Color.FromRgb(100, 90, 190), Color.FromRgb(60, 130, 210), new Point(0.5, 0), new Point(0.5, 1));
            m_GradientActiveBrush = new LinearGradientBrush(Color.FromRgb(170, 130, 220), Color.FromRgb(90, 170, 230), new Point(0.5, 1), new Point(0.5, 0));
            m_GradientInactiveBrush = new LinearGradientBrush(Color.FromRgb(61, 58, 58), Color.FromRgb(68, 68, 68), new Point(0.5, 1), new Point(0.5, 0));


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
            if (CommandHelper.IsAudioFile(file))
            {
                PlaybackManager.Stop();
                var pi = new PlaybackInfo(StackId)
                {
                    MediaFilePath = file,
                    StageType = StageType.Audio,
                    IsExclusive = PlaybackInfo.IsExclusive
                };
                PlaybackInfo = pi;

                TextBlockAudioFileName.Text = System.IO.Path.GetFileNameWithoutExtension(file);
                stageType = PlaybackStage.StageType.Audio;
                audioPlayer.MediaFilePath = audioFile = file;
            }
            else if (CommandHelper.IsVideoFile(file))
            {
                PlaybackManager.Stop();
                var pi = new PlaybackInfo(StackId)
                {
                    MediaFilePath = file,
                    StageType = StageType.Video,
                    IsExclusive = PlaybackInfo.IsExclusive
                };
                PlaybackInfo = pi;

                TextBlockAudioFileName.Text = System.IO.Path.GetFileNameWithoutExtension(file);
                stageType = PlaybackStage.StageType.Video;
                videoPlayer.MediaFilePath = videoFile = file;
            }
            else if (PlaybackInfo.StageType != StageType.None)
            {
                SystemSounds.Exclamation.Play();
            }
            else
            {
                SystemSounds.Exclamation.Play();
                TextBlockAudioFileName.Text = "Error: Unsuported format!";
                PlaybackInfo.StageType = StageType.None;
            }

            InvalidateStageState();

            //InputAudioFile.Text = fileList[0];
            // For example add all files into a simple label control:
            //foreach (string File in FileList)
            //    this.DropLocationLabel.Content += File + "\n";
        }

        private void InvalidateStageState()
        {
            switch (stageType)
            {
                case StageType.None:
                    ImageAudio.Opacity = 0d;
                    ImageVideo.Opacity = 0d;
                    ImageNone.Opacity = 100d;
                    Border.Background = m_GradientInactiveBrush;
                    break;
                case StageType.Audio:
                    ImageAudio.Opacity = 100d;
                    ImageVideo.Opacity = 0d;
                    ImageNone.Opacity = 0d;
                    Border.Background = m_GradientBrush;
                    break;
                case StageType.Video:
                    ImageVideo.Opacity = 100d;
                    ImageAudio.Opacity = 0d;
                    ImageNone.Opacity = 0d;
                    Border.Background = m_GradientBrush;
                    break;
            }

            if (PlaybackInfo != null)
                CheckBoxStopOther.IsChecked = PlaybackInfo.IsExclusive;
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
                Border.Background = m_GradientHoverBrush;
            }
            else
            {
                PlaybackManager.Play();
                Border.Background = m_GradientActiveBrush;

                OnPlay?.Invoke(this, new PlaybackStageEventArgs(PlaybackState.Paused));
            }

            Border.Background = m_GradientActiveBrush;

            playbackState = PlaybackState.Playing;
        }

        public void Stop()
        {
            PlaybackManager?.Stop();

            //if (stageType == StageType.Audio)
            //    audioPlayer.Stop();

            //if (stageType == StageType.Video)
            //    videoPlayer.Stop();


            Border.Background = m_GradientBrush;

            playbackState = PlaybackState.Stoped;

            ///System.Windows.SystemColors.MenuHighlightBrush;
        }

        void PlaybackStage_OnStateChanged(PlaybackStage sender, PlaybackStageEventArgs e)
        {
            if (sender == this)
            { }
            else if (playbackState == PlaybackState.Playing && sender.playbackState == PlaybackState.Playing && sender.CheckBoxStopOther.IsChecked.Value == true)
            {
                //Stop();
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
                    if (CommandHelper.IsAudioFile(file) || CommandHelper.IsVideoFile(file))
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

        private void CheckBoxStopOther_Click(object sender, RoutedEventArgs e)
        {
            if (PlaybackInfo != null)
                PlaybackInfo.IsExclusive = CheckBoxStopOther.IsChecked.Value;
        }

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

            if (audioPlayer != null && audioPlayer.StageType == StageType.Audio && audioPlayer != null && !String.IsNullOrEmpty(audioPlayer.MediaFilePath))
                LoadFileToStack(audioPlayer.MediaFilePath);

            if (videoPlayer != null && videoPlayer.StageType == StageType.Video && videoPlayer != null && !String.IsNullOrEmpty(videoPlayer.MediaFilePath))
                LoadFileToStack(videoPlayer.MediaFilePath);
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Border.Background = m_GradientHoverBrush;

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
                Border.Background = m_GradientActiveBrush;
            }
            else if (PlaybackInfo.StageType != StageType.None)
            {
                Border.Background = m_GradientBrush;
            }
            else
            {
                Border.Background = m_GradientInactiveBrush;
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

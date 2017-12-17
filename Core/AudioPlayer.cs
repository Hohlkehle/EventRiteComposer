using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Flac;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using EventRiteComposer.NAudio.AudioPlayback;
using System.Windows.Threading;
using System.ComponentModel;
using EventRiteComposer.Data;
using System.Windows;

namespace EventRiteComposer.Core
{
    public class AudioPlayer : IPlayable
    {
        public static event EventHandler OnTryPlay, OnPlay, OnStop;
        private IWavePlayer waveOut;
        private string m_AudioFile = null;
        private WaveStream audioFileReader;
        private Action<float> InvokeSetVolumeDelegate;
        private BackgroundWorker WorkerThread = new BackgroundWorker();
        private DispatcherTimer dispatcherTimer;
        private IOutputDevicePlugin SelectedOutputDevicePlugin;
        private bool m_IsRelativePath;
        private PlaybackInfo m_PlaybackInfo;


        public IWavePlayer WaveOutPlayer
        {
            get { return waveOut; }
            set { waveOut = value; }
        }

        public WaveStream AudioFileReader
        {
            get { return audioFileReader; }
            set { audioFileReader = value; }
        }

        public bool IsRelativePath
        {
            get { return m_IsRelativePath; }
            set { m_IsRelativePath = value; }
        }

        public bool IsPlaying => (waveOut != null && waveOut.PlaybackState == PlaybackState.Playing);

        public string AudioTrackTitle { get { return string.Format("{0}", System.IO.Path.GetFileName(AudioFile)); } }
        public string AudioFile
        {
            get
            {
                var path = m_AudioFile;
                //if (IsRelativePath)
                //{
                //    path = System.IO.Path.Combine(
                //      System.IO.Path.GetDirectoryName(MainWindow.ProjectFileName),
                //      m_AudioFile);
                //}
                return path;
            }
            set
            {
                m_AudioFile = value;
                LoadTrackTags();
            }
        }
        public FadeInOutSampleProvider FadeInOutSampleProvider { get; set; }
        public PlaybackInfo PlaybackInfo { get => m_PlaybackInfo; set => m_PlaybackInfo = value; }
        public string TrackTitle { get; private set; }
        public TimeSpan CurrentTime
        {
            get
            {
                if (waveOut != null && audioFileReader != null)
                    return (waveOut.PlaybackState == PlaybackState.Stopped) ? TimeSpan.Zero : audioFileReader.CurrentTime;
                else
                    return TimeSpan.Zero;
            }
        }

        public TimeSpan TotalTime
        {
            get
            {
                if (waveOut != null && audioFileReader != null)
                    return audioFileReader.TotalTime;
                else
                    return TimeSpan.Zero;
            }
        }

        public double Progress
        {
            get
            {
                if (waveOut != null && audioFileReader != null)
                    return Math.Min(100, (int)(100 * ((waveOut.PlaybackState == PlaybackState.Stopped) ? TimeSpan.Zero : audioFileReader.CurrentTime).TotalSeconds / audioFileReader.TotalTime.TotalSeconds));
                else
                    return 0;
            }
        }

        public float Volume = 0.5f;

        public AudioPlayer(PlaybackInfo playbackInfo)
        {
            PlaybackInfo = playbackInfo;
            AudioFile = playbackInfo.MediaFilePath;

            /*OnPlay += (object sender, EventArgs e) =>
            {
                var apcItem = (AudioPlayer)sender;
                if (!IsExclusivePlayback && !apcItem.IsExclusivePlayback && apcItem != this && waveOut != null && waveOut.PlaybackState == PlaybackState.Playing)
                {
                    ButtonStopCommand_Click(null, null);
                }

                if (apcItem == this)
                {
                    BorderContour.Visibility = Visibility.Visible;
                    //BorderBackground.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC002db3"));
                }
            };

            OnStop += (object sender, EventArgs e) =>
            {
                var apcItem = (AudioPlaybackControl)sender;

                if (apcItem == this)
                {
                    BorderContour.Visibility = Visibility.Collapsed;
                    //BorderBackground.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1141418"));
                }
            };*/
            ////Yes you can. Create the directory as normal then just set the attributes on it. E.g.

            ////DirectoryInfo di = new DirectoryInfo(@"C:\SomeDirectory");

            ////See if directory has hidden flag, if not, make hidden
            //if ((di.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            //{   
            //     //Add Hidden flag    
            //     di.Attributes |= FileAttributes.Hidden;    
            //}





            //InitializeBackgroundWorker();

            //PlayCommand = new DelegateCommand(Play);
            //StopCommand = new DelegateCommand(Stop);

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 150);

        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (waveOut != null && audioFileReader != null)
            {
                TimeSpan currentTime = (waveOut.PlaybackState == PlaybackState.Stopped) ? TimeSpan.Zero : audioFileReader.CurrentTime;


                if (waveOut.PlaybackState == PlaybackState.Stopped)
                {

                }
            }
            else
            {

            }
        }

        private void LoadTrackTags()
        {
            if (String.IsNullOrEmpty(AudioFile) || !System.IO.File.Exists(AudioFile))
                return;

            TrackTitle = string.Format("{0}", System.IO.Path.GetFileName(AudioFile));
        }

        private void OnOpenFileClick(object sender, EventArgs e)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            string allExtensions = "*.wav;*.aiff;*.mp3;*.aac;*.flac";
            openFileDialog.Filter = String.Format("All Supported Files|{0}|All Files (*.*)|*.*", allExtensions);
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AudioFile = openFileDialog.FileName;
            }
        }

        private void CreateWaveOut()
        {
            CloseWaveOut();
            var latency = Preferences.RequestedLatency;//(int)comboBoxLatency.SelectedItem;
            waveOut = SelectedOutputDevicePlugin.CreateDevice(latency);
            waveOut.PlaybackStopped += OnPlaybackStopped;
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            //groupBoxDriverModel.Enabled = true;
            if (e.Exception != null)
            {
                MessageBox.Show(e.Exception.Message, "Playback Device Error");
            }
            if (audioFileReader != null)
            {
                audioFileReader.Position = 0;
            }

            //ButtonPauseCommand.Visibility = System.Windows.Visibility.Collapsed;
            //ButtonPlayCommand.Visibility = System.Windows.Visibility.Visible;
            //if (FadeInOutSampleProvider != null)
            //{
            //    ButtonPlay1Command.Visibility = System.Windows.Visibility.Collapsed;
            //    ButtonPause1Command.Visibility = System.Windows.Visibility.Collapsed;
            //}

            //dispatcherTimer.Stop();

            //SliderSeek.Value = ProgressBarPosition.Value = 0;
        }

        public void CloseWaveOut()
        {
            if (waveOut != null)
            {
                waveOut.Stop();
            }
            if (audioFileReader != null)
            {
                // this one really closes the file and ACM conversion
                audioFileReader.Dispose();
                InvokeSetVolumeDelegate = null;
                audioFileReader = null;
            }
            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
            }
        }

        private ISampleProvider CreateInputStream(string fileName)
        {
            if (System.IO.Path.GetExtension(fileName).ToUpper() == ".FLAC")
            {
                this.audioFileReader = new FlacReader(fileName);
            }
            else
                this.audioFileReader = new AudioFileReader(fileName);

            if (!(audioFileReader is FlacReader))
            {
                FadeInOutSampleProvider = new FadeInOutSampleProvider((ISampleProvider)audioFileReader, false);

            }

            var sampleChannel = new SampleChannel(audioFileReader, true);
            sampleChannel.PreVolumeMeter += OnPreVolumeMeter;
            this.InvokeSetVolumeDelegate = (vol) =>
            {
                sampleChannel.Volume = vol;
                if (audioFileReader is AudioFileReader)
                    ((AudioFileReader)audioFileReader).Volume = vol;

            };
            var postVolumeMeter = new MeteringSampleProvider(((audioFileReader is FlacReader)) ? sampleChannel : (ISampleProvider)FadeInOutSampleProvider);
            postVolumeMeter.StreamVolume += OnPostVolumeMeter;

            return postVolumeMeter;
        }

        void OnPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            // we know it is stereo
            //waveformPainter1.AddMax(e.MaxSampleValues[0]);
            // waveformPainter2.AddMax(e.MaxSampleValues[1]);
        }

        void OnPostVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            // we know it is stereo
            System.Threading.Tasks.Task.Factory.StartNew(new Action(() =>
            {
                //Dispatcher.Invoke(delegate
                //{
                //    // Update meter async
                //    volumeMeter1.Amplitude = e.MaxSampleValues[0];
                //});
            }));

            //volumeMeter2.Amplitude = e.MaxSampleValues[1];
        }

        #region IPlayable

        public void Play()
        {
            if (String.IsNullOrEmpty(AudioFile) || !System.IO.File.Exists(AudioFile))
                OnOpenFileClick(null, null);

            if (String.IsNullOrEmpty(AudioFile))
                return;

            switch (Preferences.OutputDevice)
            {
                case "WaveOut":
                    for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
                    {
                        var capabilities = WaveOut.GetCapabilities(deviceId);
                    }

                    WaveCallbackStrategy strategy = WaveCallbackStrategy.NewWindow;
                    if (Preferences.WaveOutCallback == "Function")
                        strategy = WaveCallbackStrategy.FunctionCallback;
                    if (Preferences.WaveOutCallback == "Window")
                        strategy = WaveCallbackStrategy.NewWindow;
                    if (Preferences.WaveOutCallback == "Event")
                        strategy = WaveCallbackStrategy.Event;

                    SelectedOutputDevicePlugin = new WaveOutPlugin(strategy, Preferences.WaveOutDevice);

                    break;
                case "WasapiOut":
                    var endPoints = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    SelectedOutputDevicePlugin = new WasapiOutPlugin(
                        endPoints[Preferences.WasapiOutDevice],
                        Preferences.WasapiOutExclusiveMode == "True" ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared,
                        Preferences.WasapiOutIsEventCallback == "True",
                        Preferences.RequestedLatency);

                    break;

                case "NullOut":
                    string output;
                    string player = MainWindow.iniFile.GetString("Externals", "AudioPlayer", "C:\\Program Files (x86)\\AIMP2\\AIMP2.exe");
                    if (!player.Contains(":"))
                    {
                        //player = System.IO.Path.Combine(Environment.CurrentDirectory, player);
                    }
                    FileExecutionHelper.ExecutionHelper.RunCmdCommand(
                        "\"" + player + "\" " +
                        "\"" + AudioFile + "\"", out output, false, 1251);
                    return;
                //break;
                case "DirectSound":
                default:
                    SelectedOutputDevicePlugin = new DirectSoundOutPlugin();
                    break;
            }




            if (!SelectedOutputDevicePlugin.IsAvailable)
            {
                MessageBox.Show("The selected output driver is not available on this system");
                return;
            }

            if (waveOut != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    return;
                }
                else if (waveOut.PlaybackState == PlaybackState.Paused)
                {
                    System.Threading.Tasks.Task.Factory.StartNew(new Action(() =>
                    {
                        waveOut.Play();
                    }));

                    return;
                }
            }

            // we are in a stopped state
            try
            {
                CreateWaveOut();

                dispatcherTimer.Start();

                //Dispatcher.Invoke(delegate {
                LoadTrackTags();
                //});

            }
            //catch (TagLib.CorruptFileException tagLibCorruptFileException)
            //{
            //    Dispatcher.Invoke(delegate { TextBlockTrackTitle.Text = string.Format("{0}  {1}", System.IO.Path.GetFileName(AudioFile), tagLibCorruptFileException.Message); });
            //}
            catch (Exception driverCreateException)
            {
                MessageBox.Show(String.Format("driverCreateException {0}", driverCreateException.Message));
                return;
            }

            ISampleProvider sampleProvider = null;
            try
            {
                sampleProvider = CreateInputStream(AudioFile);
            }
            catch (Exception createException)
            {
                MessageBox.Show(String.Format("createException {0}", createException.Message), "Error Loading File");
                return;
            }

            try
            {
                //if (m_FadeInOutSampleProvider != null && !(audioFileReader is FlacReader))
                //    waveOut.Init(m_FadeInOutSampleProvider);
                //else
                waveOut.Init(sampleProvider);
            }
            catch (Exception initException)
            {
                MessageBox.Show(String.Format("initException {0}", initException.Message), "Error Initializing Output");
                return;
            }


            //Dispatcher.Invoke(delegate {
            InvokeSetVolumeDelegate((float)Volume);
            //});
            System.Threading.Tasks.Task.Factory.StartNew(new Action(() =>
            {
                waveOut.Play();
            }));

            //try
            //{
            //    System.Threading.Tasks.Task.Factory.StartNew(new Action(() =>
            //    {
            //        InitWaveformPainter(sampleProvider);
            //    }));

            //}
            //catch (Exception waveFormException)
            //{
            //    MessageBox.Show(String.Format("waveFormException {0}", waveFormException.Message), "Error Initializing Output");
            //    return;
            //}
        }

        public void Pause()
        {
            if (waveOut != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Pause();
                }
            }
        }

        public void Stop()
        {
            if (waveOut != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Stop();
                }
            }
        }

        public void SeekToTime(double time)
        {
            if (waveOut == null) return;
                audioFileReader.CurrentTime = TimeSpan.FromSeconds(audioFileReader.TotalTime.TotalSeconds * time / 100.0); 
        }

        public void SetVolume(double volume)
        {
            Volume = (float)volume;
            InvokeSetVolumeDelegate?.Invoke(Volume);
        }

        public void SetMedia(string file)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

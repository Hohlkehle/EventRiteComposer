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
using EventRiteComposer.WPFSoundVisualizationLibrary;
using WPFSoundVisualizationLib;

namespace EventRiteComposer.Core
{
    public class AudioPlayer : IPlayable, ISpectrumPlayer, IWaveformPlayer
    {
        public static event EventHandler OnTryPlay, OnPlay, OnStop;
        public event PropertyChangedEventHandler PropertyChanged;

        private IWavePlayer waveOut;
        private string m_AudioFile = null;
        private WaveStream audioFileReader;
        private Action<float> InvokeSetVolumeDelegate;
        private BackgroundWorker WorkerThread = new BackgroundWorker();
        private DispatcherTimer dispatcherTimer;
        private IOutputDevicePlugin SelectedOutputDevicePlugin;
        private bool m_IsRelativePath;
        private PlaybackInfo m_PlaybackInfo;
        private double channelPosition;
        private bool inChannelSet;
        private double channelLength;
        private float[] waveformData;
        private float[] fullLevelData;
        private SampleAggregator waveformAggregator;
        private readonly BackgroundWorker waveformGenerateWorker = new BackgroundWorker();
        private string pendingWaveformPath;
        private const int waveformCompressedPointCount = 2000;

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

        public double ChannelPosition
        {
            get { return channelPosition; }
            set
            {
                if (!inChannelSet)
                {
                    inChannelSet = true; // Avoid recursion
                    double oldValue = channelPosition;
                    double position = TotalTime.TotalSeconds / Math.Max(0, Math.Min(value, ChannelLength));
                    //if (!inChannelTimerUpdate && ActiveStream != null)
                    //    ActiveStream.Position = (long)((position / ActiveStream.TotalTime.TotalSeconds) * ActiveStream.Length);

                    SeekToTime(value);

                    channelPosition = value;
                    if (oldValue != channelPosition)
                        NotifyPropertyChanged("ChannelPosition");
                    inChannelSet = false;
                }
            }
        }

        public double ChannelLength
        {
            get { return channelLength; }
            protected set
            {
                double oldValue = channelLength;
                channelLength = value;
                if (oldValue != channelLength)
                    NotifyPropertyChanged("ChannelLength");
            }
        }

        public TimeSpan SelectionBegin { get => TimeSpan.Zero; set { } }
        public TimeSpan SelectionEnd { get => TimeSpan.Zero; set { } }

        public float Volume = 0.5f;

        public AudioPlayer(PlaybackInfo playbackInfo, SpectrumAnalyzer spectrumAnalyzer)
        {
            PlaybackInfo = playbackInfo;
            AudioFile = playbackInfo.MediaFilePath;
         
                spectrumAnalyzer.RegisterSoundPlayer(this);
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

            waveformGenerateWorker.DoWork += waveformGenerateWorker_DoWork;
            waveformGenerateWorker.RunWorkerCompleted += waveformGenerateWorker_RunWorkerCompleted;
            waveformGenerateWorker.WorkerSupportsCancellation = true;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (waveOut != null && audioFileReader != null)
            {
                TimeSpan currentTime = (waveOut.PlaybackState == PlaybackState.Stopped) ? TimeSpan.Zero : audioFileReader.CurrentTime;

                channelPosition = ((double)audioFileReader.Position / (double)audioFileReader.Length) * audioFileReader.TotalTime.TotalSeconds;
                NotifyPropertyChanged("ChannelPosition");

                //double oldValue
                //channelPosition = CurrentTime.TotalSeconds;
                //channelLength = value;
                //if (oldValue != channelLength)
                NotifyPropertyChanged("ChannelPosition");

                if (waveOut.PlaybackState == PlaybackState.Stopped)
                {

                }
            }
            else
            {

            }
        }

        public float[] WaveformData
        {
            get { return waveformData; }
            protected set
            {
                float[] oldValue = waveformData;
                waveformData = value;
                if (oldValue != waveformData)
                    NotifyPropertyChanged("WaveformData");
            }
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #region Waveform Generation
        private class WaveformGenerationParams
        {
            public WaveformGenerationParams(int points, string path)
            {
                Points = points;
                Path = path;
            }

            public int Points { get; protected set; }
            public string Path { get; protected set; }
        }

        private void GenerateWaveformData(string path)
        {
            if (waveformGenerateWorker.IsBusy)
            {
                pendingWaveformPath = path;
                waveformGenerateWorker.CancelAsync();
                return;
            }

            if (!waveformGenerateWorker.IsBusy && waveformCompressedPointCount != 0)
                waveformGenerateWorker.RunWorkerAsync(new WaveformGenerationParams(waveformCompressedPointCount, path));
        }

        private void waveformGenerateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (!waveformGenerateWorker.IsBusy && waveformCompressedPointCount != 0)
                    waveformGenerateWorker.RunWorkerAsync(new WaveformGenerationParams(waveformCompressedPointCount, pendingWaveformPath));
            }
        }

        private void waveformGenerateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WaveformGenerationParams waveformParams = e.Argument as WaveformGenerationParams;
            Mp3FileReader waveformMp3Stream = new Mp3FileReader(waveformParams.Path);
            WaveChannel32 waveformInputStream = new WaveChannel32(waveformMp3Stream);
            waveformInputStream.Sample += waveStream_Sample;
            ChannelLength = waveformInputStream.TotalTime.TotalSeconds;
            int frameLength = fftDataSize;
            int frameCount = (int)((double)waveformInputStream.Length / (double)frameLength);
            int waveformLength = frameCount * 2;
            byte[] readBuffer = new byte[frameLength];
            waveformAggregator = new SampleAggregator(frameLength);

            float maxLeftPointLevel = float.MinValue;
            float maxRightPointLevel = float.MinValue;
            int currentPointIndex = 0;
            float[] waveformCompressedPoints = new float[waveformParams.Points];
            List<float> waveformData = new List<float>();
            List<int> waveMaxPointIndexes = new List<int>();

            for (int i = 1; i <= waveformParams.Points; i++)
            {
                waveMaxPointIndexes.Add((int)Math.Round(waveformLength * ((double)i / (double)waveformParams.Points), 0));
            }
            int readCount = 0;
            while (currentPointIndex * 2 < waveformParams.Points)
            {
                waveformInputStream.Read(readBuffer, 0, readBuffer.Length);

                waveformData.Add(waveformAggregator.LeftMaxVolume);
                waveformData.Add(waveformAggregator.RightMaxVolume);

                if (waveformAggregator.LeftMaxVolume > maxLeftPointLevel)
                    maxLeftPointLevel = waveformAggregator.LeftMaxVolume;
                if (waveformAggregator.RightMaxVolume > maxRightPointLevel)
                    maxRightPointLevel = waveformAggregator.RightMaxVolume;

                if (readCount > waveMaxPointIndexes[currentPointIndex])
                {
                    waveformCompressedPoints[(currentPointIndex * 2)] = maxLeftPointLevel;
                    waveformCompressedPoints[(currentPointIndex * 2) + 1] = maxRightPointLevel;
                    maxLeftPointLevel = float.MinValue;
                    maxRightPointLevel = float.MinValue;
                    currentPointIndex++;
                }
                if (readCount % 3000 == 0)
                {
                    float[] clonedData = (float[])waveformCompressedPoints.Clone();
                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        WaveformData = clonedData;
                    }));
                }

                if (waveformGenerateWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                readCount++;
            }

            float[] finalClonedData = (float[])waveformCompressedPoints.Clone();
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                fullLevelData = waveformData.ToArray();
                WaveformData = finalClonedData;
            }));
            waveformInputStream.Close();
            waveformInputStream.Dispose();
            waveformInputStream = null;
            waveformMp3Stream.Close();
            waveformMp3Stream.Dispose();
            waveformMp3Stream = null;
        }

        private void waveStream_Sample(object sender, SampleEventArgs e)
        {
            waveformAggregator.Add(e.Left, e.Right);
        }
        #endregion

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

            //ISampleProvider provider = (audioFileReader is FlacReader) ? sampleChannel: (ISampleProvider)FadeInOutSampleProvider;
            ISampleProvider provider = (audioFileReader is FlacReader) ? sampleChannel: (ISampleProvider)FadeInOutSampleProvider;
            

            //var postVolumeMeter = new MeteringSampleProvider(((audioFileReader is FlacReader)) ? sampleChannel : (ISampleProvider)FadeInOutSampleProvider);
            var postVolumeMeter = new MeteringSampleProvider(provider);
            postVolumeMeter.StreamVolume += OnPostVolumeMeter;




            return provider;
        }

        private void inputStream_Sample(object sender, SampleEventArgs e)
        {
            sampleAggregator.Add(e.Left, e.Right);
           /* long repeatStartPosition = (long)((SelectionBegin.TotalSeconds / ActiveStream.TotalTime.TotalSeconds) * ActiveStream.Length);
            long repeatStopPosition = (long)((SelectionEnd.TotalSeconds / ActiveStream.TotalTime.TotalSeconds) * ActiveStream.Length);
            if (((SelectionEnd - SelectionBegin) >= TimeSpan.FromMilliseconds(repeatThreshold)) && ActiveStream.Position >= repeatStopPosition)
            {
                sampleAggregator.Clear();
                ActiveStream.Position = repeatStartPosition;
            }*/
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
        ISampleProvider sampleProvider = null;
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


                var inputStream = new WaveChannel32((WaveStream)audioFileReader);
                inputStream.Sample += inputStream_Sample;
                
                GenerateWaveformData(AudioFile);
                waveOut.Init(inputStream);
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

                
                sampleAggregator = new SampleAggregator(fftDataSize);
                PropertyChanged(this, new PropertyChangedEventArgs("IsPlaying"));
                //NAudioEngine.Instance.InitWaveOutDevice(null, AudioFile);
            }));

            ChannelLength = TotalTime.TotalSeconds;

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

                    PropertyChanged(this, new PropertyChangedEventArgs("IsPlaying"));
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

                    PropertyChanged(this, new PropertyChangedEventArgs("IsPlaying"));
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

        private SampleAggregator sampleAggregator;
        public bool GetFFTData(float[] fftDataBuffer)
        {
            sampleAggregator.GetFFTResults(fftDataBuffer);
            return IsPlaying;
        }

        private readonly int fftDataSize = (int)FFTDataSize.FFT2048;
        public int GetFFTFrequencyIndex(int frequency)
        {
            double maxFrequency;
            if (sampleProvider != null)
                maxFrequency = sampleProvider.WaveFormat.SampleRate / 2.0d;
            else
                maxFrequency = 22050; // Assume a default 44.1 kHz sample rate.
            return (int)((frequency / maxFrequency) * (fftDataSize / 2));
        }
        #endregion
    }
}

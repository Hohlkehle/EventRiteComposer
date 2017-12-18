using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventRiteComposer.Data;
using System.Windows;

namespace EventRiteComposer.Core
{
    public class VideoPlayer : IPlayable
    {
        private VideoPlaybackWindow m_VideoPlaybackWindow = null;
        private PlaybackInfo info;

        public VideoPlayer(PlaybackInfo info)
        {
            this.info = info;
        }

        public bool IsPlaying
        {
            get
            {
                if (m_VideoPlaybackWindow == null || !m_VideoPlaybackWindow.IsLoaded)
                    return false;
                else
                    return m_VideoPlaybackWindow.IsPlaying;
            }
        }

        public TimeSpan CurrentTime { get { return IsPlaying ? m_VideoPlaybackWindow.VideoPlayer.Position : TimeSpan.Zero; } }
        public TimeSpan TotalTime { get { return IsPlaying && m_VideoPlaybackWindow.VideoPlayer.NaturalDuration.HasTimeSpan ? m_VideoPlaybackWindow.VideoPlayer.NaturalDuration.TimeSpan : TimeSpan.Zero; } }
        public int Progress { get { return IsPlaying ? Math.Min(100, (int)(100 * (CurrentTime).TotalSeconds / TotalTime.TotalSeconds)) : 0; } }

        private void InitVideoWindow()
        {
            if (m_VideoPlaybackWindow == null || !m_VideoPlaybackWindow.IsLoaded)
                m_VideoPlaybackWindow = new VideoPlaybackWindow();

            m_VideoPlaybackWindow.Show();
            m_VideoPlaybackWindow.VideoFile = info.MediaFilePath;

            if (ScreenHandler.AllScreens > 1)
            {
                m_VideoPlaybackWindow.ShowOnMonitor(1);
            }

            m_VideoPlaybackWindow.WindowState = WindowState.Maximized;
            m_VideoPlaybackWindow.Volume = 0.5;
        }

        public void Pause()
        {
            if (m_VideoPlaybackWindow != null && m_VideoPlaybackWindow.IsLoaded)
            {
                m_VideoPlaybackWindow.Pause();
            }
        }

        public void Play()
        {
            InitVideoWindow();
            m_VideoPlaybackWindow.Play();
        }



        public void SeekToTime(double time)
        {
            if(IsPlaying)
                m_VideoPlaybackWindow.VideoPlayer.Position = TimeSpan.FromSeconds(TotalTime.TotalSeconds * (time / 100));
        }

        public void SetMedia(string file)
        {
            if (m_VideoPlaybackWindow != null && m_VideoPlaybackWindow.IsLoaded)
                m_VideoPlaybackWindow.VideoFile = file;
        }

        public void SetVolume(double volume)
        {
            if (m_VideoPlaybackWindow != null && m_VideoPlaybackWindow.IsLoaded)
                m_VideoPlaybackWindow.Volume = volume;
        }

        public void Stop()
        {
            if (m_VideoPlaybackWindow != null && m_VideoPlaybackWindow.IsLoaded)
            {
                m_VideoPlaybackWindow.Stop();
                m_VideoPlaybackWindow.Close();
            }
        }
    }
}

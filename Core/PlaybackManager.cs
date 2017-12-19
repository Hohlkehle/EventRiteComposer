using EventRiteComposer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventRiteComposer.Core
{
    public class PlaybackManager
    {
        private PlaybackInfo m_PlaybackInfo;
        private IPlayable m_Player;
        private ProgressDataProvider m_ProgressDataProvider;

        public PlaybackInfo PlaybackInfo { get => m_PlaybackInfo; set => m_PlaybackInfo = value; }
        public bool IsPlaying { get { return m_Player.IsPlaying; } }

        public PlaybackStage PlaybackStage { get; internal set; }

        public PlaybackManager(PlaybackInfo info, PlaybackStage playbackStage)
        {
            PlaybackInfo = info;
            PlaybackStage = playbackStage;
            if (info.StageType == PlaybackStage.StageType.Audio || info.StageType == PlaybackStage.StageType.None)
            {
                m_Player = new AudioPlayer(info, PlaybackStage.spectrumAnalyzer);
                m_ProgressDataProvider = new AudioProgressDataProvider((AudioPlayer)m_Player);
                
            }
            else if (info.StageType == PlaybackStage.StageType.Video)
            {
                m_Player = new VideoPlayer(info);
                m_ProgressDataProvider = new VideoProgressDataProvider((VideoPlayer)m_Player);
            }
        }

        public void Play()
        {
            m_Player.Play();
            MainWindow.ProgressDataProvider = m_ProgressDataProvider;
        }

        public void Pause()
        {
            m_Player.Pause();
        }

        public void Stop()
        {
            m_Player.Stop();
        }
        
        public void SeekToTime(double time)
        {
            m_Player.SeekToTime(time);
        }

        public void SetVolume(double volume)
        {
            m_Player.SetVolume(volume);
        }
    }
}

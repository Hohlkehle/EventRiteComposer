using EventRiteComposer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventRiteComposer.Data
{
    public class AudioProgressDataProvider : ProgressDataProvider
    {
        private AudioPlayer m_AudioPlayer;

        public override TimeSpan CurrentTime
        {
            get => m_AudioPlayer != null ? m_AudioPlayer.CurrentTime : TimeSpan.Zero;
            set => base.CurrentTime = value;
        }

        public override TimeSpan TotalTime
        {
            get => m_AudioPlayer != null ? m_AudioPlayer.TotalTime : TimeSpan.Zero;
            set => base.TotalTime = value;
        }

        public override double Progress
        {
            get => m_AudioPlayer != null ? m_AudioPlayer.Progress : 0;
            set => base.Progress = value;
        }

        public AudioProgressDataProvider(AudioPlayer audioPlayer)
        {
            m_AudioPlayer = audioPlayer;
        }


    }
}

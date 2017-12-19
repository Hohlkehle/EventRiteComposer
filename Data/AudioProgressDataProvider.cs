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
            get => AudioPlayer != null ? AudioPlayer.CurrentTime : TimeSpan.Zero;
            set => base.CurrentTime = value;
        }

        public override TimeSpan TotalTime
        {
            get => AudioPlayer != null ? AudioPlayer.TotalTime : TimeSpan.Zero;
            set => base.TotalTime = value;
        }

        public override double Progress
        {
            get => AudioPlayer != null ? AudioPlayer.Progress : 0;
            set => base.Progress = value;
        }
        public AudioPlayer AudioPlayer { get => m_AudioPlayer; set => m_AudioPlayer = value; }

        public AudioProgressDataProvider(AudioPlayer audioPlayer)
        {
            AudioPlayer = audioPlayer;
        }


    }
}

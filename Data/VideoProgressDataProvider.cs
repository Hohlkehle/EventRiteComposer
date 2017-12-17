using EventRiteComposer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventRiteComposer.Data
{
    public class VideoProgressDataProvider : ProgressDataProvider
    {
        private VideoPlayer m_VideoPlayer;

        public override TimeSpan CurrentTime
        {
            get => m_VideoPlayer != null ? m_VideoPlayer.CurrentTime : TimeSpan.Zero;
            set => base.CurrentTime = value;
        }

        public override TimeSpan TotalTime
        {
            get => m_VideoPlayer != null ? m_VideoPlayer.TotalTime : TimeSpan.Zero;
            set => base.TotalTime = value;
        }

        public override double Progress
        {
            get => m_VideoPlayer != null ? m_VideoPlayer.Progress : 0;
            set => base.Progress = value;
        }

        public VideoProgressDataProvider(VideoPlayer audioPlayer)
        {
            m_VideoPlayer = audioPlayer;
        }


    }
}

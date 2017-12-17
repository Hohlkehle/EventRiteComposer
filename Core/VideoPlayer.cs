using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventRiteComposer.Data;

namespace EventRiteComposer.Core
{
    public class VideoPlayer : IPlayable
    {
        private PlaybackInfo info;

        public VideoPlayer(PlaybackInfo info)
        {
            this.info = info;
        }

        public bool IsPlaying => throw new NotImplementedException();

        public TimeSpan CurrentTime { get; internal set; }
        public TimeSpan TotalTime { get; internal set; }
        public int Progress { get; internal set; }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play()
        {
            throw new NotImplementedException();
        }

        public void SeekToTime(double time)
        {
            throw new NotImplementedException();
        }

        public void SetMedia(string file)
        {
            throw new NotImplementedException();
        }

        public void SetVolume(double volume)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}

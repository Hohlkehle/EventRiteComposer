using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventRiteComposer.Core
{
    interface IPlayable
    {
        bool IsPlaying { get; }
        void Play();

        void Pause();

        void Stop();

        void SeekToTime(double time);

        void SetVolume(double volume);

        void SetMedia(string file);
    }
}

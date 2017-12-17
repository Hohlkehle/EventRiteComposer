using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventRiteComposer.Data
{
    public class PlaybackStageEventArgs
    {
        public EventRiteComposer.PlaybackStage.PlaybackState prevPlaybackState;
        public PlaybackStageEventArgs(EventRiteComposer.PlaybackStage.PlaybackState prev)
        {
            prevPlaybackState = prev;
        }
    }
}

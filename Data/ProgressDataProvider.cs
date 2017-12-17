using EventRiteComposer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventRiteComposer.Data
{
    public class ProgressDataProvider
    {
        public virtual double Progress { set; get; }
        public virtual TimeSpan TotalTime { set; get; }
        public virtual TimeSpan CurrentTime { set; get; }
    }
}

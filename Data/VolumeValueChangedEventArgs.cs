using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventRiteComposer.Data
{
    public class VolumeValueChangedEventArgs : EventArgs
    {
        readonly double m_Value = 0.5;

        public VolumeValueChangedEventArgs(double value)
        {
            m_Value = value;
        }

        public double Value { get => m_Value; }
    }
}

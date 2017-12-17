using EventRiteComposer.Data;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EventRiteComposer.NAudio.AudioPlayback
{
    [Export(typeof(IOutputDevicePlugin))]
    public class NullOutPlugin : IOutputDevicePlugin
    {
        DirectSoundOutSettingsPanel settingsPanel;
        public IWavePlayer CreateDevice(int latency)
        {
            return new DirectSoundOut(new Guid(Preferences.DirectSoundDevice.Value), latency);
        }

        IWavePlayer IOutputDevicePlugin.CreateDevice(int latency)
        {
            throw new NotImplementedException();
        }

        public UserControl CreateSettingsPanel()
        {
            this.settingsPanel = new DirectSoundOutSettingsPanel();
            return this.settingsPanel;
        }

        public string Name
        {
            get { return "NullOut"; }
        }

        public bool IsAvailable
        {
            get { return true; }
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}

﻿using EventRiteComposer.Data;
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
    class DirectSoundOutPlugin : IOutputDevicePlugin
    {
        private DirectSoundOutSettingsPanel settingsPanel;
        private bool isAvailable;

        public DirectSoundOutPlugin()
        {
            this.isAvailable = DirectSoundOut.Devices.Count() > 0;
        }

        public IWavePlayer CreateDevice(int latency)
        {
            return new DirectSoundOut(new Guid(Preferences.DirectSoundDevice.Value), latency);
            //return null;
        }

        public UserControl CreateSettingsPanel()
        {
            settingsPanel = new DirectSoundOutSettingsPanel();
            return this.settingsPanel;
            //return null;
        }

        public string Name
        {
            get { return "DirectSound"; }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
        }

        public int Priority
        {
            get { return 2; }
        }
    }
}

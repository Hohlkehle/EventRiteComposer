using EventRiteComposer.Data;
using Keyboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventRiteComposer
{
    [Serializable]
    public class VlcPlayer : PlaybackInfo
    {
        public VlcPlayer() : base("") { m_path = @"vlc\vlc.exe"; }

        public VlcPlayer(string path) : base(path) { }

        public override void Play(string fileName)
        {
            if (Process == null || Process.HasExited)
                StartPlayer();

            //Messaging.ForegroundKeyPress(process.MainWindowHandle, new Keyboard.Key(Messaging.VKeys.KEY_F11), 1);

            Task.Factory.StartNew(new Action(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    Messaging.ForegroundKeyPress(Process.MainWindowHandle, new Keyboard.Key(Messaging.VKeys.VK_VOLUME_DOWN), 1);
                }

                FileExecutionHelper.ExecutionHelper.RunCmdCommand(Path + " " + "\"" + fileName + "\"", out output, false, 1251);

                Thread.Sleep(350);

                for (int i = 0; i <= 20; i++)
                {
                    Messaging.ForegroundKeyPress(Process.MainWindowHandle, new Keyboard.Key(Messaging.VKeys.VK_VOLUME_UP), 35);
                }

                Messaging.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
            }));
        }

        public override void Stop()
        {

            if (Process == null || Process.HasExited)
                StartPlayer();

            Task.Factory.StartNew(new Action(() =>
            {
                for (int i = 0; i < 20; i++)
                    Messaging.ForegroundKeyPress(Process.MainWindowHandle, new Keyboard.Key(Messaging.VKeys.VK_VOLUME_DOWN), 20);
                
                for (int i = 0; i < 30; i++)
                    Messaging.ForegroundKeyPress(Process.MainWindowHandle, new Keyboard.Key(Messaging.VKeys.VK_VOLUME_DOWN), 1);

                base.Stop();

            }));
        }
    }
}

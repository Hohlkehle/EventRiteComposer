using Keyboard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using Utilities;

namespace EventRiteComposer.Data
{
    [Serializable]
    public class PlaybackInfo
    {
        [XmlIgnore]
        public static IniFile iniFile;
        [XmlIgnore]
        static string m_IniPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"cfg\PlaybackInfo.ini");
        [XmlIgnore]
        public string output;
        [XmlIgnore]
        private string m_mediaFilePath = "media.ogg";
        [XmlIgnore]
        protected string m_path = @"winamp\winamp.exe";
        [XmlIgnore]
        private EventRiteComposer.PlaybackStage.StageType m_StageType = EventRiteComposer.PlaybackStage.StageType.None;
        [XmlIgnore]
        private int m_StackId;
        [XmlIgnore]
        private bool m_IsExclusive = true;
        [XmlElement]
        public EventRiteComposer.PlaybackStage.StageType StageType
        {
            get { return m_StageType; }
            set { m_StageType = value; }
        }

        [XmlElement]
        public string Path
        {
            get { return m_path; }
            set { m_path = value; }
        }
        [XmlIgnore]
        public Process Process { set; get; }

        [XmlElement]
        public string MediaFilePath
        {
            get { return m_mediaFilePath; }
            set { m_mediaFilePath = value; }
        }
        [XmlElement]
        public int StackId
        {
            get { return m_StackId; }
            set { m_StackId = value; }
        }
        [XmlIgnore]
        public int KeyPressDelay { get { return MainWindow.instance.Preferences.KeyPressDelay.Value; } }
        [XmlElement]
        public bool IsExclusive { get => m_IsExclusive; set => m_IsExclusive = value; }

        public PlaybackInfo()
        {
            //IniFileInit();
        }

        public PlaybackInfo(string path)
        {
            this.Path = path;
            //IniFileInit();
        }

        public PlaybackInfo(int stackId)
        {
            StackId = stackId;
        }

        static void IniFileInit()
        {
            if (!File.Exists(m_IniPath))
            {
                var fstreem = File.Create(m_IniPath);
                fstreem.Close();

                iniFile = new IniFile(m_IniPath);
                //SaveSettings();
            }
            else if (iniFile == null)
            {
                iniFile = new IniFile(m_IniPath);
            }
        }

        public void SaveSettings(int id, Type type)
        {
            try
            {
                iniFile.WriteValue(type.Name + id, "mediaFilePath", m_mediaFilePath);
                iniFile.WriteValue(type.Name + id, "path", m_path);
                iniFile.WriteValue(type.Name + id, "stageType", (int)StageType);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка сохранения настроек. playbackinfo " + ex.Message, "Ошибка");
            }
        }

        public static PlaybackInfo Load(int id, Type type)
        {
            IniFileInit();

            PlaybackInfo player;

            if (type == typeof(VlcPlayer))
            {
                var mediaFilePath = iniFile.GetString(type.Name + id, "mediaFilePath", "media.mp3");
                var path = iniFile.GetString(type.Name + id, "path", MainWindow.instance.Preferences.VideoPlayer.Value);
                var stageType = (EventRiteComposer.PlaybackStage.StageType)iniFile.GetInt32(type.Name + id, "stageType", (int)EventRiteComposer.PlaybackStage.StageType.None);


                player = new VlcPlayer(path);
                player.StageType = stageType;
                player.MediaFilePath = mediaFilePath;
            }
            else
            {
                var mediaFilePath = iniFile.GetString(type.Name + id, "mediaFilePath", "media.mp3");
                var path = iniFile.GetString(type.Name + id, "path", MainWindow.instance.Preferences.AudioPlayer.Value);
                var stageType = (EventRiteComposer.PlaybackStage.StageType)iniFile.GetInt32(type.Name + id, "stageType", (int)EventRiteComposer.PlaybackStage.StageType.None);


                player = new PlaybackInfo(path);
                player.StageType = stageType;
                player.MediaFilePath = mediaFilePath;
            }

            return player;
        }

        public virtual void StartPlayer(bool waitForIdle = true)
        {
            var proc = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(Path));

            if (proc.Length != 0)
            {
                Process = proc[0];
                Process.StartInfo.FileName = Path;
            }
            else
            {
                Process = new Process();
                Process.StartInfo.FileName = Path;
                Process.Start();

                // Need to wait for app to start
                if (waitForIdle)
                    Process.WaitForInputIdle();
            }
        }

        public virtual void Play()
        {
            Play(MediaFilePath);
        }

        public virtual void Play(string fileName)
        {
            if (Process == null || Process.HasExited)
                StartPlayer();

            FileExecutionHelper.ExecutionHelper.RunCmdCommand(Path + " " + "\"" + fileName + "\"", out output, false, 1251);

            Messaging.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
        }

        public virtual void Stop()
        {
            if (Process == null || Process.HasExited)
                StartPlayer();


            Task.Factory.StartNew(new Action(() =>
            {
                Messaging.ForegroundKeyDown(Process.MainWindowHandle, new Keyboard.Key(Messaging.VKeys.VK_MEDIA_STOP));
                Thread.Sleep(KeyPressDelay);
                Messaging.ForegroundKeyUp(Process.MainWindowHandle, new Keyboard.Key(Messaging.VKeys.VK_MEDIA_STOP));

                Messaging.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
            }));
        }

        public void Terminate()
        {
            if (Process != null)
            {
                Process.Kill();
            }
        }

        public static void SaveSettings(object config, string filename)
        {
            return;

            using (Stream writer = new FileStream(System.IO.Path.Combine(Application.StartupPath, filename), FileMode.OpenOrCreate))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(config.GetType());
                    serializer.Serialize(writer, config);
                }
                catch (Exception e)
                {
                    //MessageBox.Show(e.Message);
                }
            }
        }

        public static PlaybackInfo LoadSettings(string filename)
        {
            return (PlaybackInfo)_LoadSettings(filename, new PlaybackInfo());
        }

        public static PlaybackInfo LoadSettings(string filename, Type type)
        {
            if (!File.Exists(System.IO.Path.Combine(Application.StartupPath, filename)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
                using (StreamWriter sw = File.CreateText(System.IO.Path.Combine(Application.StartupPath, filename))) { sw.WriteLine(""); }
                SaveSettings(Activator.CreateInstance(type), filename);
            }
            // Загружаем данные из файла
            using (Stream stream = new FileStream(System.IO.Path.Combine(Application.StartupPath, filename), FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(type);
                return (PlaybackInfo)serializer.Deserialize(stream);
            }
        }

        public static object _LoadSettings(string filename, object config)
        {
            if (!File.Exists(System.IO.Path.Combine(Application.StartupPath, filename)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
                using (StreamWriter sw = File.CreateText(System.IO.Path.Combine(Application.StartupPath, filename))) { sw.WriteLine(""); }
                SaveSettings(config, filename);
            }
            // Загружаем данные из файла
            using (Stream stream = new FileStream(System.IO.Path.Combine(Application.StartupPath, filename), FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(config.GetType());
                return (object)serializer.Deserialize(stream);
            }
        }
    }
}

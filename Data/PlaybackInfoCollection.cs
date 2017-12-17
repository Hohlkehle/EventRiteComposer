using EventRiteComposer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using IOFile = System.IO.File;
     
namespace EventRiteComposer.Data
{
    [Serializable]
    public class PlaybackInfoCollection
    {
        [XmlIgnore]
        public static PlaybackInfoCollection instance;
        [XmlIgnore]
        private List<PlaybackInfo> m_Stages = new List<PlaybackInfo>();
        [XmlElement]
        public List<PlaybackInfo> Stages
        {
            get { return m_Stages; }
            set { m_Stages = value; }
        }

        public PlaybackInfoCollection()
        {
            instance = this;
        }

        public void Populate(PlaybackInfo[] playbackInfos)
        {
            Stages.AddRange(playbackInfos);
        }

        internal PlaybackInfo GetInfoByStackId(int stackId)
        {
            return Stages.FirstOrDefault(s => s.StackId == stackId);
        }

        public static PlaybackInfoCollection Load(string filename)
        {
            if (!IOFile.Exists(filename))
                return new PlaybackInfoCollection();

            PlaybackInfoCollection collection = null;

            using (var reader = new FileStream(filename, FileMode.OpenOrCreate))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(PlaybackInfoCollection));
                    collection = (PlaybackInfoCollection)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return collection;
        }

      

        public void Save(string filename)
        {
            IOFile.Delete(filename);

            using (Stream writer = new FileStream(filename, FileMode.OpenOrCreate))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(PlaybackInfoCollection));
                    serializer.Serialize(writer, this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}

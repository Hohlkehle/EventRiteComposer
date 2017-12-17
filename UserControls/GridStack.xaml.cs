using EventRiteComposer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventRiteComposer
{
    /// <summary>
    /// Interaction logic for GridStack.xaml
    /// </summary>
    public partial class GridStack : UserControl
    {
        private PlaybackInfoCollection m_PlaybackInfoCollection;

        public GridStack()
        {
            InitializeComponent();
            m_PlaybackInfoCollection = PlaybackInfoCollection.Load(Properties.Settings.Default.GridXmlFile);
            InitializeGridStack(m_PlaybackInfoCollection);
            MainWindow.OnApplicationQuit += MainWindow_OnApplicationQuit;
            MainWindow.OnButtonStopPressed += MainWindow_OnButtonStopPressed;

        }

        private void MainWindow_OnButtonStopPressed(object sender, RoutedEventArgs e)
        {
            foreach (var ps in GridStackPanel.Children.OfType<PlaybackStage>())
            {
                ps.Stop();
            }
        }

        private void InitializeGridStack(PlaybackInfoCollection m_PlaybackInfoCollection)
        {
            if (m_PlaybackInfoCollection.Stages.Count > 0)
                foreach (var ps in GridStackPanel.Children.OfType<PlaybackStage>().OrderBy((s => s.StackId)))
                {
                    var pi = m_PlaybackInfoCollection.GetInfoByStackId(ps.StackId);
                    ps.PlaybackInfo = pi;
                }
            else
            {
                foreach (var ps in GridStackPanel.Children.OfType<PlaybackStage>().OrderBy((s => s.StackId)))
                {
                    ps.InitializePlaybackInfo(ps.StackId);
                }
            }
        }

        public List<PlaybackInfo> GridStackPanelChildrens
        {
            get
            {
                var ls = new List<PlaybackInfo>();
                foreach (var ps in GridStackPanel.Children.OfType<PlaybackStage>().OrderBy((s => s.StackId)))
                {
                    ls.Add(ps.PlaybackInfo);
                }
                return ls;
            }
        }

        private void MainWindow_OnApplicationQuit(object sender, EventArgs e)
        {
            m_PlaybackInfoCollection.Stages = GridStackPanelChildrens;
            m_PlaybackInfoCollection.Save(Properties.Settings.Default.GridXmlFile);
        }
    }
}

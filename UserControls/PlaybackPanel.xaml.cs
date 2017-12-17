using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventRiteComposer.UserControls
{
    /// <summary>
    /// Interaction logic for PlaybackPanel.xaml
    /// </summary>
    public partial class PlaybackPanel : UserControl
    {
        private DateTime sliderSeekMouseDownStart;
        private bool sliderSeekdragStarted;

        public PlaybackPanel()
        {
            InitializeComponent();
        }
        private void SliderSeek_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            //if (waveOut != null)
            //{
            //    audioFileReader.CurrentTime = TimeSpan.FromSeconds(audioFileReader.TotalTime.TotalSeconds * SliderSeek.Value / 100.0);
            //}

            //sliderSeekdragStarted = false;
        }

        private void SliderSeek_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

            if (e.ChangedButton != MouseButton.Left)
                return;

            //if (waveOut == null) return;

            //if (DateTime.Now - sliderSeekMouseDownStart > TimeSpan.FromMilliseconds(300))
            //    SliderSeek_ThumbDragCompleted(null, null);
            //else
            //{

            //    var pos = e.GetPosition(SliderSeek);

            //    SliderSeek.Value = SmoothStep(pos.X, 0, SliderSeek.ActualWidth, SliderSeek.Minimum, SliderSeek.Maximum);

            //    audioFileReader.CurrentTime = TimeSpan.FromSeconds(audioFileReader.TotalTime.TotalSeconds * SliderSeek.Value / 100.0);

            //    sliderSeekdragStarted = false;
            //}
        }

        private void SliderSeek_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            sliderSeekMouseDownStart = DateTime.Now;
            sliderSeekdragStarted = true;
        }

        private void SliderSeek_ThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            sliderSeekdragStarted = true;
        }

        private void SliderSeek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if (waveOut != null && sliderSeekdragStarted)
            //{
            //    audioFileReader.CurrentTime = TimeSpan.FromSeconds(audioFileReader.TotalTime.TotalSeconds * SliderSeek.Value / 100.0);
            //}
            //ProgressBarPosition.Value = SliderSeek.Value;
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if (InvokeSetVolumeDelegate != null)
            //{
            //    InvokeSetVolumeDelegate((float)SliderVolume.Value);
            //}

            if (ProgressBarVolume != null)
                ProgressBarVolume.Value = SliderVolume.Value;
        }

        private void ButtonPlayCommand_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonPauseCommand_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

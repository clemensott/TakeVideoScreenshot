using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures;
using Vlc.DotNet.Forms;

namespace TakeVideoScreenshot
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VlcControl vlc;
        private DateTime videoCreated;
        private TimeSpan length;

        public MainWindow()
        {
            InitializeComponent();

            me.MediaPlayer.BeginInit();
            me.MediaPlayer.VlcLibDirectory = new DirectoryInfo(@"vlc");
            me.MediaPlayer.EndInit();

            vlc = me.MediaPlayer;
            vlc.PositionChanged += Vlc_PositionChanged;
            vlc.LengthChanged += Vlc_LengthChanged;
        }

        private void Vlc_LengthChanged(object sender, VlcMediaPlayerLengthChangedEventArgs e)
        {
            length = new TimeSpan((long)e.NewLength);
        }

        private void Vlc_PositionChanged(object sender, VlcMediaPlayerPositionChangedEventArgs e)
        {
            Dispatcher.Invoke(new Action(UpdateSlider));
        }

        private void UpdateSlider()
        {
            lock (sld)
            {
                sld.Value = vlc.Position;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FileInfo file = new FileInfo(files[0]);

                videoCreated = file.LastWriteTime;

                vlc.Play(file);

            }
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            PlayPause();
        }

        private void BtnSlower_Click(object sender, RoutedEventArgs e)
        {
            SlowerDown();
        }

        private void BtnFaster_Click(object sender, RoutedEventArgs e)
        {
            SpeedUp();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            PreviousFrame();
        }

        private void BtnFor_Click(object sender, RoutedEventArgs e)
        {
            NextFrame();
        }

        private void BtnTake_Click(object sender, RoutedEventArgs e)
        {
            TakeScreenshot();
        }

        private void sld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Monitor.IsEntered(sld)) return;

            vlc.Position = (float)sld.Value;
        }

        private void me_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    PlayPause();
                    break;

                case Key.OemMinus:
                    SlowerDown();
                    break;

                case Key.OemPlus:
                    SpeedUp();
                    break;

                case Key.Q:
                    PreviousFrame();
                    break;

                case Key.E:
                    NextFrame();
                    break;

                case Key.W:
                    TakeScreenshot();
                    break;

                case Key.A:
                    Forward(TimeSpan.FromSeconds(-5));
                    break;

                case Key.S:
                    Forward(TimeSpan.FromSeconds(5));
                    break;
            }
        }

        private void PlayPause()
        {
            switch (vlc.State)
            {
                case MediaStates.Buffering:
                    vlc.Pause();
                    break;

                case MediaStates.Ended:
                    vlc.Position = 0;
                    vlc.Play();
                    break;

                case MediaStates.Opening:
                    vlc.Pause();
                    break;

                case MediaStates.Paused:
                    vlc.Play();
                    break;

                case MediaStates.Playing:
                    vlc.Pause();
                    break;

                case MediaStates.Stopped:
                    vlc.Position = 0;
                    vlc.Play();
                    break;
            }
        }

        private void SlowerDown()
        {
            vlc.Rate /= (float)1.1;
        }

        private void SpeedUp()
        {
            vlc.Rate *= (float)1.1;
        }

        private void PreviousFrame()
        {
            TimeSpan time = TimeSpan.FromDays(vlc.Position * length.TotalDays);

            if (vlc.State == MediaStates.Playing) vlc.Pause();

            TimeSpan timePerFrame = TimeSpan.FromSeconds(1 / vlc.VlcMediaPlayer.FramesPerSecond);
            TimeSpan timeOfFrameBefore = time - timePerFrame;

            vlc.Position = 0;

            vlc.Position = (float)(timeOfFrameBefore.TotalDays / length.TotalDays);
        }

        private void NextFrame()
        {
            vlc.VlcMediaPlayer.NextFrame();
        }

        private void TakeScreenshot()
        {
            string path;
            string filePrefix = tbxFilePrefix.Text;

            if (filePrefix.Length == 0)
            {
                MessageBox.Show("Check targetpath!");
                return;
            }

            TimeSpan playerTime = TimeSpan.FromDays(vlc.Position * length.TotalDays);
            DateTime captureTime = videoCreated + playerTime;

            do
            {
                string fileName = string.Format(" {1}.png", filePrefix, Convert(captureTime));
                path = filePrefix + fileName;

                captureTime = captureTime.AddMilliseconds(1);

            } while (File.Exists(path));

            Task.Factory.StartNew(new Action<object>(TakeScreenshot), path);
        }

        private static string Convert(DateTime t)
        {
            string date = string.Format("{0,2}-{1,2}-{2,2}", t.Year, t.Month, t.Day).Replace(" ", "0");
            string time = string.Format("{0,2}-{1,2}-{2,2}-{3,3}", t.Hour, t.Minute, t.Second, t.Millisecond).Replace(" ", "0");

            return date + " " + time;
        }

        private void TakeScreenshot(object path)
        {
            vlc.TakeSnapshot(path.ToString());
        }

        private void Forward(TimeSpan offset)
        {
            TimeSpan time = TimeSpan.FromDays(vlc.Position * length.TotalDays);
            TimeSpan timeTo = time + offset;

            vlc.Position = (float)(timeTo.TotalDays / length.TotalDays);
        }
    }
}

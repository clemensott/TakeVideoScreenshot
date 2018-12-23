using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures;
using Vlc.DotNet.Wpf;

namespace TakeVideoScreenshot
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime videoCreated;
        private TimeSpan length;
        private readonly RotateList<VlcControl> vlcs;
        private readonly Dictionary<Vlc.DotNet.Forms.VlcControl, string> busyVlcs;
        private readonly DispatcherTimer timer;

        public VlcControl CurrentVlcControl { get => vlcs[0]; }

        public Vlc.DotNet.Forms.VlcControl CurrentMediaPlayer { get => vlcs[0].MediaPlayer; }

        public MainWindow()
        {
            InitializeComponent();

            vlcs = new RotateList<VlcControl>();
            busyVlcs = new Dictionary<Vlc.DotNet.Forms.VlcControl, string>();
            timer = new DispatcherTimer();

            foreach (VlcControl wpfVlc in GetWpfVlcControls())
            {
                wpfVlc.MediaPlayer.BeginInit();
                wpfVlc.MediaPlayer.VlcLibDirectory = new DirectoryInfo(@"vlc");
                wpfVlc.MediaPlayer.EndInit();

                wpfVlc.Visibility = Visibility.Hidden;

                vlcs.Add(wpfVlc);
            }

            CurrentVlcControl.Visibility = Visibility.Visible;

            CurrentMediaPlayer.PositionChanged += Vlc_PositionChanged;
            CurrentMediaPlayer.LengthChanged += Vlc_LengthChanged;

            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private IEnumerable<VlcControl> GetWpfVlcControls()
        {
            yield return me1;
            yield return me2;
            yield return me3;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var pair in busyVlcs.ToArray())
            {
                if (File.Exists(pair.Value))
                {
                    busyVlcs.Remove(pair.Key);
                    System.Diagnostics.Debug.WriteLine("Remove: " + new FileInfo(pair.Value).Length);

                    if (pair.Key == CurrentMediaPlayer) CurrentMediaPlayer.Position = (float)sld.Value;
                }
            }

            if (busyVlcs.ContainsKey(CurrentMediaPlayer)) NextVlc();

            tblState.Text = busyVlcs.ContainsKey(CurrentMediaPlayer) ? "Busy" : "Ready";
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
                sld.Value = CurrentMediaPlayer.Position;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FileInfo file = new FileInfo(files[0]);

                videoCreated = file.LastWriteTime;

                foreach (VlcControl vlc in vlcs)
                {
                    vlc.MediaPlayer.SetMedia(file);
                }

                CurrentMediaPlayer.Play();
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

        private void Sld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Monitor.IsEntered(sld)) return;

            CurrentMediaPlayer.Position = (float)sld.Value;
        }

        private void Me_KeyDown(object sender, KeyEventArgs e)
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
            switch (CurrentMediaPlayer.State)
            {
                case MediaStates.Buffering:
                    CurrentMediaPlayer.Pause();
                    break;

                case MediaStates.Ended:
                    CurrentMediaPlayer.Position = 0;
                    CurrentMediaPlayer.Play();
                    break;

                case MediaStates.Opening:
                    CurrentMediaPlayer.Pause();
                    break;

                case MediaStates.Paused:
                    CurrentMediaPlayer.Play();
                    break;

                case MediaStates.Playing:
                    CurrentMediaPlayer.Pause();
                    break;

                case MediaStates.Stopped:
                    CurrentMediaPlayer.Position = 0;
                    CurrentMediaPlayer.Play();
                    break;

                default:
                    CurrentMediaPlayer.Position = (float)sld.Value;
                    CurrentMediaPlayer.Play();
                    break;
            }
        }

        private void SlowerDown()
        {
            CurrentMediaPlayer.Rate /= (float)1.1;
        }

        private void SpeedUp()
        {
            CurrentMediaPlayer.Rate *= (float)1.1;
        }

        private void PreviousFrame()
        {
            TimeSpan time = TimeSpan.FromDays(CurrentMediaPlayer.Position * length.TotalDays);

            if (CurrentMediaPlayer.State == MediaStates.Playing) CurrentMediaPlayer.Pause();

            TimeSpan timePerFrame = TimeSpan.FromSeconds(1 / CurrentMediaPlayer.VlcMediaPlayer.FramesPerSecond);
            TimeSpan timeOfFrameBefore = time - timePerFrame;

            CurrentMediaPlayer.Position = 0;
            CurrentMediaPlayer.Position = (float)(timeOfFrameBefore.TotalDays / length.TotalDays);
        }

        private void NextFrame()
        {
            CurrentMediaPlayer.VlcMediaPlayer.NextFrame();
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

            TimeSpan playerTime = TimeSpan.FromDays(CurrentMediaPlayer.Position * length.TotalDays);
            DateTime captureTime = videoCreated + playerTime;

            do
            {
                string fileName = string.Format(" {1}.png", filePrefix, Convert(captureTime));
                path = filePrefix + fileName;

                captureTime = captureTime.AddMilliseconds(1);

            } while (File.Exists(path));


            Task.Factory.StartNew(() => TakeScreenshot(CurrentMediaPlayer, path));

            NextVlc();
        }

        private static string Convert(DateTime t)
        {
            string date = string.Format("{0,2}-{1,2}-{2,2}", t.Year, t.Month, t.Day).Replace(" ", "0");
            string time = string.Format("{0,2}-{1,2}-{2,2}-{3,3}", t.Hour, t.Minute, t.Second, t.Millisecond).Replace(" ", "0");

            return date + " " + time;
        }

        private void TakeScreenshot(Vlc.DotNet.Forms.VlcControl vlc, string path)
        {
            System.Diagnostics.Debug.WriteLine("Take: " + vlc.GetHashCode());

            busyVlcs.Add(vlc, path);
            while (!File.Exists(path)) vlc.TakeSnapshot(path);

            System.Diagnostics.Debug.WriteLine("Took: " + vlc.GetHashCode());
        }

        private void Forward(TimeSpan offset)
        {
            TimeSpan time = TimeSpan.FromDays(CurrentMediaPlayer.Position * length.TotalDays);
            TimeSpan timeTo = time + offset;

            CurrentMediaPlayer.Position = (float)(timeTo.TotalDays / length.TotalDays);
        }

        private void NextVlc()
        {
            bool isPlayling = CurrentMediaPlayer.IsPlaying;
            float position = CurrentMediaPlayer.Position;

            CurrentMediaPlayer.Pause();

            CurrentMediaPlayer.PositionChanged -= Vlc_PositionChanged;
            CurrentMediaPlayer.LengthChanged -= Vlc_LengthChanged;

            CurrentVlcControl.Visibility = Visibility.Hidden;

            for (int i = 0; i < vlcs.Count; i++)
            {
                vlcs.Next();

                if (!busyVlcs.ContainsKey(CurrentMediaPlayer)) break;
            }

            CurrentVlcControl.Visibility = Visibility.Visible;

            CurrentMediaPlayer.PositionChanged += Vlc_PositionChanged;
            CurrentMediaPlayer.LengthChanged += Vlc_LengthChanged;

            System.Diagnostics.Debug.WriteLine("NextVlc: " + CurrentMediaPlayer.GetHashCode());
            System.Diagnostics.Debug.WriteLine(busyVlcs.ContainsKey(CurrentMediaPlayer));
            System.Diagnostics.Debug.WriteLine(CurrentMediaPlayer.IsPlaying);

            if (isPlayling && !busyVlcs.ContainsKey(CurrentMediaPlayer))
            {
                CurrentMediaPlayer.Play();

                CurrentMediaPlayer.Position = 0;
                CurrentMediaPlayer.Position = position;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            timer.Stop();

            base.OnClosing(e);
        }
    }
}

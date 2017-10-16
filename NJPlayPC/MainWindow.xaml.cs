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
using System.IO;
using System.Windows.Threading;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using WinForms = System.Windows.Forms;
using System.Xml.Serialization;

namespace NJPlayPC
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer activityTimer;
        DispatcherTimer timerTs;
        TimeSpan activityThreshold = TimeSpan.FromSeconds(3);
        bool isHidden = false;
        bool isPlaying = false;
        bool isFullscreen = false;
        bool isMaximized;
        double h;
        double w;
        private TcpClient client;
        public StreamReader STR;
        public StreamWriter STW;
        public string receive;
        public String input_to_send;
        public BackgroundWorker worker1 = new BackgroundWorker();
        public BackgroundWorker worker2 = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            IPAddress[] localIP = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in localIP)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    sIP.Text = address.ToString();
                }
            }

            worker1.DoWork += Worker1_DoWork;
            worker2.DoWork += worker2_DoWork;

            activityTimer = new DispatcherTimer();
            activityTimer.Tick += new EventHandler(activityWorker_Tick);
            activityTimer.Interval = TimeSpan.FromMilliseconds(100);
            activityTimer.Start();

            timerTs = new DispatcherTimer();
            timerTs.Interval = TimeSpan.FromMilliseconds(500);
            timerTs.Tick += new EventHandler(Timer_Tick);
            mediaElement1.LoadedBehavior = MediaState.Manual;
            mediaElement1.UnloadedBehavior = MediaState.Manual;
        }

        private void activityWorker_Tick(object sender, EventArgs e)
        {
            bool shouldHide = User32GetIdle.GetLastInput() > activityThreshold;
            if (isHidden != shouldHide)
            {
                if (shouldHide)
                {
                    this.Cursor = Cursors.None;
                    btnPlay.Visibility = Visibility.Hidden;
                    btnPause.Visibility = Visibility.Hidden;
                    btnFullscreen.Visibility = Visibility.Hidden;
                    sliderSeek.Visibility = Visibility.Hidden;
                    sliderVolume.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.Cursor = Cursors.Arrow;
                    btnPlay.Visibility = Visibility.Visible;
                    btnPause.Visibility = Visibility.Visible;
                    btnFullscreen.Visibility = Visibility.Visible;
                    sliderSeek.Visibility = Visibility.Visible;
                    sliderVolume.Visibility = Visibility.Visible;
                }
                isHidden = shouldHide;
            }
        }

            private void Timer_Tick(object sender, EventArgs e)
        {
            sliderSeek.Value = mediaElement1.Position.TotalSeconds;
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            mediaElement1.Play();
            timerTs.Start();
            isPlaying = true;
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaElement1.Pause();
            timerTs.Stop();
            isPlaying = false;
        }

        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaElement1.Volume = (double)sliderVolume.Value;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string dropFilename = (string)((DataObject)e.Data).GetFileDropList()[0];
            mediaElement1.LoadedBehavior = MediaState.Manual;
            mediaElement1.UnloadedBehavior = MediaState.Manual;
            mediaElement1.Source = new Uri(dropFilename);
            sliderSeek.Value = 0;
            mediaElement1.Position = TimeSpan.FromTicks(0);
            mediaElement1.Play();
            timerTs.Start();
            isPlaying = true;
        }

        private void sliderSeek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (User32GetIdle.GetLastInput() <= TimeSpan.FromMilliseconds(500))
            {
                mediaElement1.Pause();
                timerTs.Stop();
                mediaElement1.Position = TimeSpan.FromSeconds(sliderSeek.Value);
                mediaElement1.Play();
                timerTs.Start();
            }
            if (sliderSeek.Value == sliderSeek.Maximum)
            {
                sliderSeek.Value = 0;
                mediaElement1.Position = TimeSpan.FromTicks(0);
            }
            else { return; }
        }

        private void mediaElement1_MediaOpened(object sender, RoutedEventArgs e)
        {
            TimeSpan ts = mediaElement1.NaturalDuration.TimeSpan;
            sliderSeek.Maximum = ts.TotalSeconds;
            mediaElement1.LoadedBehavior = MediaState.Manual;
            mediaElement1.UnloadedBehavior = MediaState.Manual;
        }

        private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (!isFullscreen)
            {
                if (WindowState == WindowState.Maximized)
                {
                    isMaximized = true;
                }
                else
                {
                    isMaximized = false;
                    w = Application.Current.MainWindow.Width;
                    h = Application.Current.MainWindow.Height;
                }
                WindowState = WindowState.Normal;
                Application.Current.MainWindow.Height = 720;
                Application.Current.MainWindow.Width = 1280;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
                isFullscreen = true;
            }
            else
            {
                ResizeMode = ResizeMode.CanResize;
                if (!isMaximized)
                {
                    WindowState = WindowState.Normal;
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    Application.Current.MainWindow.Height = h;
                    Application.Current.MainWindow.Width = w;
                }
                else
                {
                    WindowState = WindowState.Maximized;
                    WindowStyle = WindowStyle.SingleBorderWindow;
                }
                isFullscreen = false;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                sliderVolume.Value = sliderVolume.Value + (sliderVolume.LargeChange / 2);
            }
            if (e.Key == Key.Left)
            {
                sliderVolume.Value = sliderVolume.Value - (sliderVolume.LargeChange / 2);
            }
            if (e.Key == Key.Space)
            {
                if (isPlaying)
                {
                    mediaElement1.Pause();
                    timerTs.Stop();
                    isPlaying = false;
                }
                else
                {
                    mediaElement1.Play();
                    timerTs.Start();
                    isPlaying = true;
                }
            }
            return;
        }

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            StartServerPrompt.Visibility = Visibility.Collapsed;
            btnConnect.Visibility = Visibility.Collapsed;
            TcpListener listener = new TcpListener(IPAddress.Any, int.Parse(sPort.Text));
            listener.Start();
            client = listener.AcceptTcpClient();
            STR = new StreamReader(client.GetStream());
            STW = new StreamWriter(client.GetStream());
            STW.AutoFlush = true;

            worker1.RunWorkerAsync();
            worker2.WorkerSupportsCancellation = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            StartServerPrompt.Visibility = Visibility.Collapsed;
            sPort.Text = String.Empty;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            StartServerPrompt.Visibility = Visibility.Visible;
        }

        private void worker2_DoWork(object sender, DoWorkEventArgs e)
        {
            STW.WriteLine(input_to_send);
            STW.Flush();
            input_to_send = "";
            worker2.CancelAsync();
        }

        private void Worker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (client.Connected)
            {
                receive = STR.ReadLine();
                if (receive == "connected")
                {
                    MessageBox.Show("Controler connected", "NJPlay", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                if (STR.ReadLine() == "vDown")
                {
                    sliderVolume.Value = sliderVolume.Value - (sliderVolume.LargeChange / 2);
                }
                if (STR.ReadLine() == "vUp")
                {
                    sliderVolume.Value = sliderVolume.Value + (sliderVolume.LargeChange / 2);
                }
                if (STR.ReadLine() == "skip")
                {
                    mediaElement1.Pause();
                    timerTs.Stop();
                    mediaElement1.Position = TimeSpan.FromSeconds(sliderSeek.Value + sliderSeek.Value / 20);
                    mediaElement1.Play();
                    timerTs.Start();
                }
                if (STR.ReadLine() == "return")
                {
                    mediaElement1.Pause();
                    timerTs.Stop();
                    mediaElement1.Position = TimeSpan.FromSeconds(sliderSeek.Value - sliderSeek.Value / 20);
                    mediaElement1.Play();
                    timerTs.Start();
                }
                if (STR.ReadLine() == "playpause")
                {
                    if (isPlaying)
                    {
                        mediaElement1.Pause();
                        timerTs.Stop();
                        isPlaying = false;
                    }
                    else
                    {
                        mediaElement1.Play();
                        timerTs.Start();
                        isPlaying = true;
                    }
                }
            }
        }
    }
}
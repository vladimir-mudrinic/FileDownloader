using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FileDownloader;
using static System.Windows.MessageBoxButton;
using static System.Windows.MessageBoxImage;

namespace ImageDownloaderWpfClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Downloader downloader = new Downloader();
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    poster.Source = null;

                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    tokenSource = null;
                    downloader.Cancel();

                    Uri requestUri = new Uri(urlTextBox.Text);
                    Progress<double> progress = new Progress<double>();
                    progress.ProgressChanged += Progress_ProgressChanged;

                    tokenSource = new CancellationTokenSource();

                    Stream stream = await downloader.DownloadAsync(requestUri, tokenSource.Token, progress);

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    poster.Source = bitmap;
                }
            }
            catch (UriFormatException ex)
            {
                ShowError(ex.Message);
            }
            catch (DownloadException ex)
            {
                ShowError(ex.InnerException.Message);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message);
            }
        }

        private void Progress_ProgressChanged(object sender, double progress)
        {
            progresUProcentu.Text = progress.ToString("P");
            progres.Value = progress * 100;
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Greška", OK, Error);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
            downloader.Cancel();
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;using FileDownloader;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ImageDownloaderUwpClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Downloader downloader = new Downloader();
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public MainPage()
        {
            InitializeComponent();
        }

        private async void UrlTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (e.Key == VirtualKey.Enter)
                {
                    poster.Source = null;

                    tokenSource.Cancel();

                    Uri requestUri = new Uri(urlTextBox.Text);
                    Progress<double> progress = new Progress<double>();
                    progress.ProgressChanged += Progress_ProgressChanged;

                    InMemoryRandomAccessStream ras = new InMemoryRandomAccessStream();

                    tokenSource = new CancellationTokenSource();

                    Stream stream = await downloader.DownloadAsync(requestUri, tokenSource.Token, progress);

                    await stream.CopyToAsync(ras.AsStreamForWrite());

                    ras.Seek(0);

                    BitmapImage bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(ras);
                    poster.Source = bitmap;
                }
            }
            catch (UriFormatException ex)
            {
                ShowErrorAsync(ex.Message);
            }
            catch (DownloadException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (ArgumentException ex)
            {
                ShowErrorAsync(ex.Message);
            }
            catch (IOException)
            {
            }
        }

        private void Progress_ProgressChanged(object sender, double progress)
        {
            progresUProcentu.Text = progress.ToString("P1", new CultureInfo("sr"));
            progres.Value = progress * 100;
        }

        private static async void ShowErrorAsync(string message)
        {
            await new MessageDialog(message, "Error").ShowAsync();
        }
    }
}

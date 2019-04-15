using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Http.HttpCompletionOption;
using static System.StringComparison;

namespace FileDownloader
{
    public class Downloader
    {
        private HttpClient _httpClient = new HttpClient();

        /// <summary>Downloads a file from remote location.</summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <param name="progress"></param>
        /// <exception cref="DownloadException">Thrown when download has failed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when download has failed.</exception>
        public async Task<Stream> DownloadAsync(Uri requestUri, CancellationToken cancellationToken, IProgress<double> progress)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUri, ResponseHeadersRead, cancellationToken);
                if (!response.Content.Headers.ContentType.MediaType.StartsWith("image", OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Uri does not represent an uri of an image.", nameof(requestUri));
                }
                MemoryStream memoryStream;
                long? contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue)
                {
                    Stream contentAsStream = await response.Content.ReadAsStreamAsync();
                    int length = Convert.ToInt32(contentLength.Value);
                    DownloadTracker tracker = new DownloadTracker(length);
                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        progress.Report(tracker.Percentage);
                        tracker.ReadBytes = await contentAsStream.ReadAsync(tracker.Buffer, tracker.Downloaded, tracker.Remaining, cancellationToken);
                    } while (tracker.MakeProgress());
                    memoryStream = new MemoryStream(tracker.Buffer);
                }
                else
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream, 81920, cancellationToken);
                    progress.Report(1.0);
                }

                cancellationToken.ThrowIfCancellationRequested();

                return memoryStream;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new DownloadException("Object from requested Uri is not reporting correct size. It is either negative or too low.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new DownloadException("", ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
                progress.Report(0);
            }
        }

        /// <summary>Cancel all pending downloads on this instance.</summary>
        public void Cancel()
        {
            //REMARK: After calling this method, the HttpClient instance can still be used to execute additional requests.
            _httpClient.CancelPendingRequests();
        }

        /// <summary>
        /// 
        /// </summary>
        private class DownloadTracker
        {
            private readonly int _downloadLength;
            private readonly byte[] buffer;

            /// <summary></summary>
            /// <param name="contentLength">Total bytes to download</param>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when download has failed.</exception>
            public DownloadTracker(int contentLength)
            {
                if (contentLength < 0)
                {
                    throw new ArgumentOutOfRangeException("A value must be non-negative.");
                }

                buffer = new byte[contentLength];

                Remaining = _downloadLength = contentLength;
            }

            /// <summary>
            /// 
            /// </summary>
            public int Remaining
            {
                get;
                private set;
            }

            /// <summary>
            /// Total bytes that have been downloaded so far.
            /// </summary>
            /// <value>Total bytes that have been downloaded so far.</value>
            public int Downloaded
            {
                get;
                private set;
            }

            /// <summary>
            /// 
            /// </summary>
            public double Percentage
            {
                get;
                private set;
            }

            /// <summary>
            /// 
            /// </summary>
            public byte[] Buffer => buffer;

            /// <summary></summary>
            /// <value></value>
            public int ReadBytes
            {
                get;
                internal set;
            }

            /// <summary>Recalcutes remaining bytes for download.</summary>
            /// <returns>Returns true if progress have been made, else returns false</returns>
            /// <exception cref="InvalidOperationException">Thrown when download has failed.</exception>
            public bool MakeProgress()
            {
                if (ReadBytes == 0)
                {
                    return false;
                }

                if (Remaining == 0)
                {
                    throw new InvalidOperationException("All data has been downloaded.");
                }

                if (ReadBytes < 0 || ReadBytes > Remaining)
                {
                    throw new InvalidOperationException("A passed value is negative or it would cause initially set value for total bytes to be exceeded.");
                }

                Downloaded += ReadBytes;
                Remaining -= ReadBytes;

                Percentage = 1.0 * Downloaded / _downloadLength;

                return true;
            }
        }
    }
}

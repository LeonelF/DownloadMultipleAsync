using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileCatelog
{
    partial class DownloadClient
    {
        HttpClient httpClient;
        public bool isProcessCancel = false;
        public DownloadClient()
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(30);
            }
        }

        public async Task DownloadFileAsync(string url, IProgress<double> progress, CancellationToken token, string fileDirectoryPath)
        {
            using (HttpResponseMessage response = httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result)
            {
                response.EnsureSuccessStatusCode();

                //Get total content length
                var total = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : -1L;
                var canReportProgress = total != -1 && progress != null;
                using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(fileDirectoryPath + url.Substring(url.LastIndexOf('/') + 1), FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 8192, true))
                {
                    var totalRead = 0L;
                    var totalReads = 0L;
                    var buffer = new byte[8192];
                    var isMoreToRead = true;

                    do
                    {
                        var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            await fileStream.WriteAsync(buffer, 0, read);

                            totalRead += read;
                            totalReads += 1;

                            //if (canReportProgress)
                            //{
                            //    //Check if operation is cancelled by user
                            //    if (!isProcessCancel)
                            //    {
                            //        //total bytes downloaded so far in totalRead
                            //        progress.Report((totalRead * 1d) / (total * 1d) * 100);
                            //    }
                            //    else
                            //    {
                            //        progress.Report(100);
                            //    }
                            //}
                            if (totalReads % 2000 == 0 || canReportProgress)
                            {
                                //Check if operation is cancelled by user
                                if (!isProcessCancel)
                                {
                                    progress.Report((totalRead * 1d) / (total * 1d) * 100);
                                }
                                else
                                {
                                    progress.Report(100);
                                }
                            }
                        }
                    }
                    while (isMoreToRead);
                }
            }
        }

        public void CancelFileAsync()
        {
            if (httpClient.BaseAddress != null)
            {
                httpClient.CancelPendingRequests();
                httpClient.Dispose();
            }
        }
    }
}

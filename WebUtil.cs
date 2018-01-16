using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Datasheets2
{
    public static class WebUtil
    {
        public static async Task<MemoryStream> RequestStreamAsync(Uri url, CancellationToken ct, Dictionary<string, string> headers = null)
        {
            Debug.WriteLine($"Fetch URL: {url}");

            var content = new MemoryStream();
            var request = (HttpWebRequest)WebRequest.Create(url);

            // Apply headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    switch (header.Key)
                    {
                        case "Referer":
                            request.Referer = header.Value;
                            break;

                        default:
                            HttpRequestHeader headerName;
                            if (Enum.TryParse(header.Key, out headerName))
                                request.Headers.Add(headerName, header.Value);
                            else
                                request.Headers.Add(header.Key, header.Value);
                            break;
                    }
                }
            }

            using (WebResponse response = await request.GetResponseAsync())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    await responseStream.CopyToAsync(content);
                }
            }

            content.Seek(0, SeekOrigin.Begin);
            return content;
        }

        public static async Task<byte[]> RequestBytesAsync(Uri url, CancellationToken ct, Dictionary<string, string> headers = null)
        {
            const int MAX_RESPONSE_LENGTH = 10 * 1024 * 1024; // 10 MiB
            var content = await RequestStreamAsync(url, ct, headers);

            var buffer = new byte[Math.Min(content.Length, MAX_RESPONSE_LENGTH)];
            int bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, ct);
            return buffer;
        }

        public static async Task<string> RequestStringAsync(Uri url, CancellationToken ct, Dictionary<string, string> headers = null)
        {
            var bytes = await RequestBytesAsync(url, ct, headers);
            // TODO: Detect encoding from response
            //return Encoding.ASCII.GetString(bytes);
            return Encoding.UTF8.GetString(bytes);


        }

        public static async Task<HtmlDocument> RequestHtmlAsync(Uri url, CancellationToken ct, Dictionary<string, string> headers = null)
        {
            var content = await RequestStreamAsync(url, ct, headers);

            var doc = new HtmlDocument();
            doc.Load(content);
            
            return doc;
        }
    }
}

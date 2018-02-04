using HtmlAgilityPack;
using Newtonsoft.Json;
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
        public const string FakeUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0";

        public static async Task<HttpWebResponse> WebRequestAsync(
            Uri url, 
            CancellationToken ct = default(CancellationToken), 
            Dictionary<string, string> headers = null, 
            CookieContainer cookieJar = null)
        {
            Debug.WriteLine($"Fetch URL: {url}");
            var request = (HttpWebRequest)WebRequest.Create(url);

            if (cookieJar != null)
                request.CookieContainer = cookieJar;

            request.UserAgent = WebUtil.FakeUserAgent;

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

                        case "Accept":
                            request.Accept = header.Value;
                            break;

                        case "UserAgent":
                            request.UserAgent = header.Value;
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

            return (HttpWebResponse)await request.GetResponseAsync();
        }

        public static async Task<byte[]> RequestBytesAsync(Uri url, CancellationToken ct = default(CancellationToken), Dictionary<string, string> headers = null)
        {
            const int MAX_RESPONSE_LENGTH = 10 * 1024 * 1024; // 10 MiB

            using (var response = await WebRequestAsync(url, ct, headers))
            {
                var content = await MemoryStreamFromWebResponseAsync(response, ct);

                var buffer = new byte[Math.Min(content.Length, MAX_RESPONSE_LENGTH)];
                int bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, ct);
                return buffer;
            }
        }

        public static async Task<string> RequestStringAsync(Uri url, CancellationToken ct = default(CancellationToken), Dictionary<string, string> headers = null)
        {
            var bytes = await RequestBytesAsync(url, ct, headers);
            // TODO: Detect encoding from response
            //return Encoding.ASCII.GetString(bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        public static async Task<HtmlDocument> RequestHtmlAsync(Uri url, CancellationToken ct = default(CancellationToken), Dictionary<string, string> headers = null)
        {
            using (var response = await WebRequestAsync(url, ct, headers))
            {
                var content = await MemoryStreamFromWebResponseAsync(response, ct);

                var doc = new HtmlDocument();
                doc.Load(content);

                return doc;
            }
        }

        public static async Task<T> RequestJsonAsync<T>(Uri url, CancellationToken ct = default(CancellationToken), Dictionary<string, string> headers = null)
        {
            /*using (var response = await WebRequestAsync(url, ct, headers))
            {
                var content = await MemoryStreamFromWebResponseAsync(response, ct);

                using (var reader = new JsonTextReader(new StreamReader(content)))
                {
                    await reader.ReadAsync();
                    return reader.Value;
                }
            }*/

            var content = await RequestStringAsync(url, ct, headers);
            return JsonConvert.DeserializeObject<T>(content);
        }


        public static async Task<MemoryStream> MemoryStreamFromWebResponseAsync(WebResponse response, CancellationToken ct = default(CancellationToken))
        {
            var content = new MemoryStream();
            using (var responseStream = response.GetResponseStream())
            {
                await responseStream.CopyToAsync(content);
            }

            content.Seek(0, SeekOrigin.Begin);
            return content;
        }

        public static async Task<HtmlDocument> HtmlFromWebResponseAsync(WebResponse response, CancellationToken ct = default(CancellationToken))
        {
            var stream = await MemoryStreamFromWebResponseAsync(response, ct);

            var doc = new HtmlDocument();
            doc.Load(stream);

            return doc;
        }


        /// <summary>
        /// Save web response to a file
        /// </summary>
        /// <param name="response"></param>
        /// <param name="destpath"></param>
        /// <param name="fileMode"></param>
        /// <returns></returns>
        public static async Task SaveResponseToFileAsync(WebResponse response, string destpath, FileMode fileMode = FileMode.Create)
        {
            // TODO: Extension from content type?
            using (var stream = response.GetResponseStream())
            {
                using (var filestream = new FileStream(destpath, fileMode))
                {
                    await stream.CopyToAsync(filestream);
                }
            }
        }

        /// <summary>
        /// Copy cookies from one URL to another.
        /// Required as a work-around for CookieContainer's weird behaviour.
        /// Also sets Version=0 as recommended by various forum posts
        /// </summary>
        /// <param name="cookies"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public static void CopyCookies(CookieContainer cookies, Uri source, Uri dest)
        {
            foreach (Cookie cookie in cookies.GetCookies(source))
            {
                cookies.Add(dest, new Cookie
                {
                    Name = cookie.Name,
                    Version = 0,
                    Comment = cookie.Comment,
                    CommentUri = cookie.CommentUri,
                    Discard = cookie.Discard,
                    //Domain = cookie.Domain,
                    Domain = dest.Host,
                    Expired = cookie.Expired,
                    Expires = cookie.Expires,
                    HttpOnly = cookie.HttpOnly,
                    //Path = cookie.Path,
                    Path = dest.LocalPath,
                    Port = cookie.Port,
                    Secure = cookie.Secure,
                    Value = cookie.Value
                });
            }
        }
    }
}

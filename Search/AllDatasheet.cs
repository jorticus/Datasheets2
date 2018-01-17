using Datasheets2.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Datasheets2.Search
{
    public class AllDatasheet : ISearchProvider
    {
        private const string ENDPOINT_QUERY = "http://www.alldatasheet.com/view.jsp?Searchword={0}";


        public async Task SearchAsync(string query, CancellationToken ct)
        {
            // Maximum results to return
            const int N_RESULTS = 3;
            HashSet<Uri> uriset = new HashSet<Uri>();
            HashSet<string> partNoSet = new HashSet<string>();

            Debug.WriteLine($"AllDatasheet: BeginSearch");
            try
            {
                var uri = new Uri(String.Format(ENDPOINT_QUERY, Uri.EscapeDataString(query)));

                CookieContainer cookieJar = new CookieContainer();
                HtmlDocument html;
                using (var response = await WebRequestAsync(uri, cookieJar, ct))
                    html = await WebUtil.HtmlFromWebResponseAsync(response, ct);

                //There's no easy way to do this.
                // First we look for the first table with class="main" and a header containing "Electronic Manufacturer"
                var table = html.DocumentNode.SelectSingleNode("/html/body//table[@class='main']//td[@width=180 and contains(., 'Electronic Manufacturer')]/../..");
                if (table == null)
                    return;

                // Skip header
                var rows = table.SelectNodes("tr")?.Skip(1);
                if (rows == null)
                    return;

                // Now we can extract the PDF links
                int count = 0;
                foreach (var row in rows)
                {
                    // Since some rows have an image with a rowspan, we search for all td's containing an <a href=""><b>text</b></a> sequence:
                    var partElem = row.SelectSingleNode("td/a/b/..");
                    var partNo = partElem?.InnerText?.Trim();
                    var href = partElem?.Attributes["href"]?.Value;
                    
                    // The description is the last column
                    var description = row.SelectNodes("td")?.LastOrDefault()?.InnerText;

                    Debug.WriteLine($"AllDatasheet: {partNo}, {href}");

                    Uri itemUri = null;
                    Uri.TryCreate(href, UriKind.Absolute, out itemUri);

                    if (!string.IsNullOrWhiteSpace(partNo) && itemUri != null)
                    {
                        // Ignore entries with duplicate URLs
                        if (!uriset.Contains(itemUri) && !partNoSet.Contains(partNo))
                        {
                            // Request URL of the actual PDF file
                            // TODO: Lazy load this on request to prevent spamming server
                            var dsUri = await RequestDatasheetUri(itemUri, uri, ct, cookieJar);
                            if (dsUri != null)
                            {
                                OnItemFound(new WebSearchItem
                                {
                                    DatasheetUrl = dsUri,
                                    PartName = partNo,
                                    Description = description
                                });

                                // Limit to N valid results
                                if (++count == N_RESULTS)
                                    break;
                            }
                        }

                        uriset.Add(itemUri);
                        partNoSet.Add(partNo);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                throw new Exception("AllDatasheet: Error retrieving results\n"+ex.Message, innerException: ex);
            }
        }

        private async Task<Uri> RequestDatasheetUri(Uri uri, Uri referer, CancellationToken ct, CookieContainer cookieJar)
        {
            HtmlDocument html;
            // Page 1 : Metadata & Download Link
            /*{
                using (var response = await WebRequestAsync(uri, cookieJar, ct, referer))
                    html = await WebUtil.HtmlFromWebResponseAsync(response, ct);

                var table = html.DocumentNode.SelectSingleNode("//table[@id='AutoNumber1']");
                if (table == null)
                    return uri;

                var rows = table.SelectNodes("tr/td/table[@class='preview']/tr");
                if (rows == null)
                    return uri;

                var thumburl = table.SelectNodes("tr/td/a/img")?.FirstOrDefault()?.Attributes["src"];

                var partNo = rows.ElementAtOrDefault(0)?.SelectSingleNode("td[2]")?.InnerText?.Trim();
                var downloadurl = rows.ElementAtOrDefault(1)?.SelectSingleNode("td/a")?.Attributes["href"]?.Value;
                var size = rows.ElementAtOrDefault(3)?.SelectSingleNode("td[2]")?.InnerText?.Trim();
                var pages = rows.ElementAtOrDefault(5)?.SelectSingleNode("td[2]")?.InnerText?.Trim();
                var manufacturer = rows.ElementAtOrDefault(7)?.SelectSingleNode("td[2]")?.InnerText?.Trim();
                var manufHomepageUrl = rows.ElementAtOrDefault(9)?.SelectSingleNode("td[2]")?.InnerText?.Trim();
                var manufLogoUrl = rows.ElementAtOrDefault(11).SelectSingleNode("td[2]/img")?.Attributes["src"]?.Value;

                // Navigate to download page
                referer = uri;
                if (!Uri.TryCreate(downloadurl, UriKind.Absolute, out uri))
                    return uri;
            }*/

            // Instead of going through the download page, you can just modify the URL like so:
            uri = new Uri(uri.AbsoluteUri.Replace(
                "www.alldatasheet.com/datasheet-pdf/pdf/", 
                "pdf1.alldatasheet.com/datasheet-pdf/view/"));

            // Page 2 : PDF Viewer
            {
                using (var response = await WebRequestAsync(uri, cookieJar, ct, referer))
                    html = await WebUtil.HtmlFromWebResponseAsync(response, ct);

                var pdfsrc = html.DocumentNode.SelectSingleNode("//iframe[@name=333]")?.Attributes["src"]?.Value;
                if (pdfsrc == null)
                    return uri;

                Uri pdfuri;
                if (!Uri.TryCreate(pdfsrc, UriKind.Relative, out pdfuri))
                    return uri;

                Uri.TryCreate(uri, pdfuri, out uri);
                return uri;
            }
        }

        public async Task DownloadPdfAsync(ISearchResult item, string destpath, CancellationToken ct)
        {
            //var response = WebRequestAsync(item.DatasheetUrl, ct: ct);
            // TODO: Save to file
            throw new NotImplementedException();
        }

        private async Task<HttpWebResponse> WebRequestAsync(Uri uri, CookieContainer cookieJar = null, CancellationToken ct = default(CancellationToken), Uri referer = null)
        {
            var request = WebRequest.CreateHttp(uri);
            request.CookieContainer = cookieJar;

            if (referer != null)
                request.Referer = referer.AbsoluteUri;

            request.UserAgent = WebUtil.FakeUserAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            return (HttpWebResponse)await request.GetResponseAsync();
        }


        public event EventHandler<ItemFoundEventArgs> ItemFound;

        private void OnItemFound(WebSearchItem item)
        {
            ItemFound?.Invoke(this, new ItemFoundEventArgs(item));
        }
    }
}

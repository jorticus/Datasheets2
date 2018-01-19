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
    /// <summary>
    /// http://alldatasheet.com Datasheet Search
    /// This website modifies the PDF title to contain "ALLDATASHEET"
    /// </summary>
    public class AllDatasheet : ISearchProvider
    {
        private const string ENDPOINT_QUERY = "http://www.alldatasheet.com/view.jsp?Searchword={0}";

        public string Name { get { return "AllDatasheet.com"; } }

        protected class AllDatasheetSearchResult : WebSearchItem
        {
            public CookieContainer CookieJar { get; set; }

            public override async Task DownloadDatasheetAsync(string destpath, CancellationToken ct = default(CancellationToken))
            {
                // Use custom webrequest with previously captured cookies
                using (var response = await ((AllDatasheet)Provider).WebRequestAsync(DatasheetUrl, CookieJar, ct))
                {
                    await WebUtil.SaveResponseToFileAsync(response, destpath);
                }
            }
        }

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

                //WebUtil.CopyCookies(cookieJar, "www.alldatasheet.com", ".alldatasheet.com");

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
                    var partNo = HtmlEntity.DeEntitize(partElem?.InnerText)?.Trim();
                    var href = partElem?.Attributes["href"]?.Value;
                    
                    // The description is the last column
                    var description = HtmlEntity.DeEntitize(row.SelectNodes("td")?.LastOrDefault()?.InnerText)?.Trim();

                    Debug.WriteLine($"AllDatasheet: {partNo}, {href}, {description}");

                    Uri itemUri = null;
                    Uri.TryCreate(href, UriKind.Absolute, out itemUri);

                    if (!string.IsNullOrWhiteSpace(partNo) && itemUri != null)
                    {
                        // Ignore entries with duplicate URLs
                        if (!uriset.Contains(itemUri) && !partNoSet.Contains(partNo))
                        {
                            // Request URL of the actual PDF file
                            // TODO: Lazy load this on request to prevent spamming server
                            //CookieContainer cookieJar2 = new CookieContainer();
                            var dsUri = await RequestDatasheetUri(itemUri, uri, ct, cookieJar);
                            if (dsUri != null)
                            {
                                OnItemFound(new AllDatasheetSearchResult
                                {
                                    DatasheetUrl = dsUri,
                                    PartName = partNo,
                                    Description = description,
                                    Provider = this,
                                    CookieJar = cookieJar
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

            //WebUtil.CopyCookies(cookieJar, "www.alldatasheet.com", "pdf1.alldatasheet.com");

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

                if (!Uri.TryCreate(uri, pdfuri, out pdfuri))
                    return uri;

                // Copy cookies from the webpage to the eventual PDF page since otherwise it doesn't seem to get them.
                // Not sure why I need to do this, probably a bug in the CookieContainer.
                WebUtil.CopyCookies(cookieJar, uri, pdfuri);

                return pdfuri;
            }
        }

        private Task<HttpWebResponse> WebRequestAsync(Uri uri, CookieContainer cookieJar = null, CancellationToken ct = default(CancellationToken), Uri referer = null)
        {
            var headers = new Dictionary<string, string>();

            headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            // Not sure if required
            if (referer != null)
                headers["Referer"] = referer.AbsoluteUri;

            return WebUtil.WebRequestAsync(uri, ct, headers, cookieJar);
        }


        public event EventHandler<ItemFoundEventArgs> ItemFound;

        private void OnItemFound(WebSearchItem item)
        {
            ItemFound?.Invoke(this, new ItemFoundEventArgs(item));
        }
    }
}

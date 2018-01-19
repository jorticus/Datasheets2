using Datasheets2.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Datasheets2.Search
{
    /// <summary>
    /// http://datasheetcatalog.net Datasheet Search
    /// This website adds an extra page to the end of the PDF file, containing a link back to the site.
    /// </summary>
    public class DatasheetCatalog : ISearchProvider
    {
        private const string ENDPOINT_QUERY = "http://search.datasheetcatalog.net/key/{0}";

        public string Name { get { return "DatasheetCatalog.net"; } }

        protected class DatasheetCatalogSearchResult : WebSearchItem
        {
            public override async Task DownloadDatasheetAsync(string destpath, CancellationToken ct = default(CancellationToken))
            {
                var cookies = new CookieContainer();
                cookies.Add(DatasheetUrl, new Cookie("fromsite", "true")); // heh.

                using (var response = await WebUtil.WebRequestAsync(DatasheetUrl, ct, cookieJar: cookies))
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

            Debug.WriteLine($"DatasheetCatalog: BeginSearch");
            try
            {
                var uri = new Uri(String.Format(ENDPOINT_QUERY, Uri.EscapeDataString(query)));

                var html = await WebUtil.RequestHtmlAsync(uri, ct);

            
                // "Datasheets found :: <N>"
                var summary = html.DocumentNode.SelectSingleNode("/html/body/center/div[1]/center/table[2]/tr/td[2]/font/font[2]")?.InnerText;
                int nResults = 0;
                Int32.TryParse(summary, out nResults);

                Debug.WriteLine($"DatasheetCatalog: Found {nResults} results");
                if (nResults > 0)
                {
                    // Table of results
                    var table = html.DocumentNode.SelectNodes("/html/body/center/div[1]/center/table[3]/tr");

                    // Skip header, enumerate rows
                    int count = 0;
                    foreach (var row in table.Skip(1))
                    {
                        var n = HtmlEntity.DeEntitize(row.SelectSingleNode("td[1]")?.InnerText);
                        var partName = HtmlEntity.DeEntitize(row.SelectSingleNode("td[2]")?.InnerText);
                        var description = HtmlEntity.DeEntitize(row.SelectSingleNode("td[3]")?.InnerText);
                        var manufacturer = HtmlEntity.DeEntitize(row.SelectSingleNode("td[4]")?.InnerText);
                        var url = row.SelectSingleNode("td[2]/a")?.Attributes["href"]?.Value;

                        Debug.WriteLine($"DatasheetCatalog: {n}, {partName}, {description}, {manufacturer}, {url}");

                        // Ignore entries that don't actually match our part name query
                        // (the webpage may match the description, which is not what we want)
                        if (!partName.ToLowerInvariant().Contains(query.ToLowerInvariant()))
                            continue;

                        Uri itemUri = null;
                        Uri.TryCreate(url, UriKind.Absolute, out itemUri);

                        if (!string.IsNullOrWhiteSpace(partName) && itemUri != null)
                        {
                            // Ignore entries with duplicate URLs
                            if (!uriset.Contains(itemUri))
                            {
                                uriset.Add(itemUri);

                                // Request URL of the actual PDF file
                                // TODO: Lazy load this on request to prevent spamming server
                                var dsUri = await RequestDatasheetUri(itemUri, ct);
                                if (dsUri != null)
                                {
                                    OnItemFound(new DatasheetCatalogSearchResult
                                    {
                                        DatasheetUrl = dsUri,
                                        PartName = partName,
                                        Description = description,
                                        Manufacturer = manufacturer,
                                        Provider = this
                                    });

                                    // Limit to N valid results
                                    if (++count == N_RESULTS)
                                        return;
                                }
                            }
                        }
                           
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DatasheetCatalog: Error retrieving results", innerException: ex);
            }

            return;
        }

        /// <summary>
        /// Follow the link provided in the main table to find the actual PDF URL
        /// </summary>
        /// <param name="uri">URL of the detail HTML page</param>
        /// <param name="ct"></param>
        /// <returns>URL of the PDF, or null</returns>
        private async Task<Uri> RequestDatasheetUri(Uri uri, CancellationToken ct)
        {
            Debug.WriteLine($"DatasheetCatalog: Request Datasheet");

            var html = await WebUtil.RequestHtmlAsync(uri, ct);

            // "Download <Part Name> from <Manufactuer>"
            // "javascript:openreq('http://pdf.datasheetcatalog.com/datasheet/microchip/39617a.pdf')"
            var href = html.DocumentNode.SelectSingleNode("/html/body/center/center/table[2]/tr/td[3]/font/font/a")?.Attributes["href"]?.Value;

            if (!string.IsNullOrWhiteSpace(href))
            {
                var m = Regex.Match(href, @"javascript:openreq\('(.+)'\)");
                if (m.Success)
                {
                    href = m.Groups[1].Value;

                    //Uri dsUri = null;
                    Uri dsUri = uri;
                    Uri.TryCreate(href, UriKind.Absolute, out dsUri);
                    return dsUri;
                }
            }
            return null;
        }

        public event EventHandler<ItemFoundEventArgs> ItemFound;

        private void OnItemFound(WebSearchItem item)
        {
            ItemFound?.Invoke(this, new ItemFoundEventArgs(item));
        }
    }
}

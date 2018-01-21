using Datasheets2.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;

namespace Datasheets2.Models
{
    /// <summary>
    /// Generic/Base class for search results
    /// </summary>
    /// <remarks>
    /// Can be overidden if you need specific behaviour for downloading the item
    /// </remarks>
    public class SearchResult : ISearchResult
    {
        public SearchResult(ISearchProvider provider)
        {
            this.Provider = provider;
        }

        public string PartName { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }

        public Uri WebpageUrl { get; set; }
        public Uri DatasheetUrl { get; set; }

        public ISearchProvider Provider { get; set; }

        public virtual async Task DownloadDatasheetAsync(string destpath, CancellationToken ct = default(CancellationToken))
        {
            // Default. Can be overridden by search provider if needed
            using (var response = await WebUtil.WebRequestAsync(DatasheetUrl, ct))
            {
                await WebUtil.SaveResponseToFileAsync(response, destpath);
            }
        }


    }
}

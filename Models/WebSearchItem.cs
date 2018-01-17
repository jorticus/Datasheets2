using Datasheets2.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Datasheets2.Models
{
    public class WebSearchItem : ISearchResult
    {
        public WebSearchItem(Uri url = null, string label = null, string description = null, ISearchProvider provider = null)
        {
            this.DatasheetUrl = url;
            this.PartName = label;
            this.Description = description;
            this.Provider = provider;
        }

        public string PartName { get; set; }
        public Uri DatasheetUrl { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }

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

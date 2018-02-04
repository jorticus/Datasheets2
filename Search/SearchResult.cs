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

        /// <summary>
        /// Part name (eg. PIC18F2550)
        /// </summary>
        public string PartName { get; set; }

        /// <summary>
        /// A description for the part
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The manufacturer name (eg. Microchip)
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// A webpage for the search result
        /// </summary>
        public Uri WebpageUrl { get; set; }

        /// <summary>
        /// The datasheet file
        /// </summary>
        public Uri DatasheetUrl { get; set; }

        /// <summary>
        /// Attribution for the datasheet (optional)
        /// </summary>
        public string DatasheetSource { get; set; }

        /// <summary>
        /// Datasheet file size, if known (optional)
        /// </summary>
        public int? DatasheetFileSize { get; set; } // In bytes

        /// <summary>
        /// Number of pages, if known (optional)
        /// </summary>
        public int? DatasheetPages { get; set; }


        /// <summary>
        /// A file name that represents the part
        /// </summary>
        public virtual string Filename { get { return ShellOperation.SanitizeFilename(PartName + ".pdf"); } }

        /// <summary>
        /// The search provider that generated this search result
        /// </summary>
        public ISearchProvider Provider { get; set; }

        /// <summary>
        /// Default method for downloading the datasheet file
        /// </summary>
        /// <param name="destpath">Where to download it to</param>
        /// <param name="ct"></param>
        /// <returns></returns>
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

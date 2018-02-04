using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Datasheets2.Search
{
    public interface ISearchResult
    {
        string PartName { get; }
        string Description { get; }
        string Manufacturer { get; }

        Uri WebpageUrl { get; }
        Uri DatasheetUrl { get; }

        string DatasheetSource { get; set; }
        int? DatasheetFileSize { get; set; } // In bytes
        int? DatasheetPages { get; set; }

        string Filename { get; }

        Task DownloadDatasheetAsync(string destpath, CancellationToken ct = default(CancellationToken));
    }
}

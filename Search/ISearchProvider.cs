using Datasheets2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Datasheets2.Search
{
    public class ItemFoundEventArgs : EventArgs
    {
        public ISearchResult Item { get; private set; }

        public ItemFoundEventArgs(ISearchResult item)
        {
            this.Item = item;
        }
    }

    public interface ISearchResult
    {
        string PartName { get; }
        string Description { get; }
        string Manufacturer { get; }
        Uri DatasheetUrl { get; }

        Task DownloadDatasheetAsync(string destpath, CancellationToken ct = default(CancellationToken));
    }

    public interface ISearchProvider
    {
        /// <summary>
        /// Begin a search using the search provider
        /// </summary>
        /// <param name="query">The query term to search</param>
        /// <param name="ct">Token to cancel the search with</param>
        Task SearchAsync(string query, CancellationToken ct);

        /// <summary>
        /// Event called when a new item is retrieved from the search
        /// </summary>
        event EventHandler<ItemFoundEventArgs> ItemFound;
    }
}

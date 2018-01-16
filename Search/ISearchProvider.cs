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
        public WebSearchItem Item { get; private set; }

        public ItemFoundEventArgs(WebSearchItem item)
        {
            this.Item = item;
        }
    }

    public interface ISearchProvider
    {
        //void BeginSearch(string query);
        //void CancelSearch();
        Task SearchAsync(string query, CancellationToken ct);

        event EventHandler<ItemFoundEventArgs> ItemFound;
    }
}

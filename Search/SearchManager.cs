using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Datasheets2.Search
{
    public class SearchManager
    {
        private async Task ProviderSearchAsync<T>(string query, CancellationToken ct)
        {
            try
            {
                Type providerType = typeof(T);
                ISearchProvider provider = (ISearchProvider)Activator.CreateInstance(providerType);

                provider.ItemFound += Provider_ItemFound;
                try
                {
                    await provider.SearchAsync(query, ct);
                }
                finally
                {
                    provider.ItemFound -= Provider_ItemFound;
                    provider = null;
                }
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (WebException ex)
            {
                App.ErrorHandler(ex.ToString(), "HTTP Error", fatal: false);
            }
            catch (Exception ex)
            {
                App.ErrorHandler(ex, fatal: false);
            }
        }

        public async Task<bool> SearchAsync(string query, CancellationToken ct)
        {
            try
            {
                // Search primary providers
                OnStatusChanged("Searching Octopart");
                await Task.WhenAll(
                    ProviderSearchAsync<Search.OctopartAPI>(query, ct)
                );

                // Search secondary providers
                OnStatusChanged("Searching DatasheetCatalog,AllDatasheet");
                await Task.WhenAll(
                    ProviderSearchAsync<Search.DatasheetCatalog>(query, ct),
                    ProviderSearchAsync<Search.AllDatasheet>(query, ct)
                );

                OnStatusChanged("Done!");
                return true; // Results found
            }
            catch (TaskCanceledException)
            {
                OnStatusChanged("Cancelled");
                return false; // No results
            }
        }

        private void Provider_ItemFound(object sender, ItemFoundEventArgs e)
        {
            ItemFound?.Invoke(sender, e);
        }

        private void OnStatusChanged(string status)
        {
            //StatusChanged?.Invoke(null, new EventArgs)
        }

        public event EventHandler<ItemFoundEventArgs> ItemFound;
        public event EventHandler StatusChanged;
    }
}

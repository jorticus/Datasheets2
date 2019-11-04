using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Datasheets2.Search
{
    public class SearchManager
    {
        private List<Type> AvailableProviders;
        private List<Type> EnabledProviders;
        private int numItemsFound;

        public SearchManager()
        {
            // Find all classes that implement ISearchProvider
            this.AvailableProviders = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => t.IsClass && typeof(ISearchProvider).IsAssignableFrom(t))
                .ToList();

            foreach (var p in AvailableProviders)
                Debug.WriteLine($"Available Provider: {p.Name}");

            // Filter providers by config
            var enabledProviderNames = Settings.SearchProviders;
            this.EnabledProviders = AvailableProviders
                .Where(p => enabledProviderNames.Contains(p.Name))
                .ToList();

            // Detect mis-named providers in the config
            if (this.EnabledProviders.Count < enabledProviderNames.Count)
            {
                var availableNames = AvailableProviders.Select(p => p.Name);
                var unmatchedNames = enabledProviderNames
                    .Where(n => !availableNames.Contains(n));

                string items = String.Join(",", unmatchedNames);
                App.ErrorHandler($"One or more providers specified in the config are invalid: [{items}]", fatal: true);
            }

            Debug.WriteLine(Settings.AllowOnlineSearch ? "Online search enabled" : "Online search disabled");
        }

        private Task ProviderSearchAsync<T>(string query, CancellationToken ct)
        {
            Type providerType = typeof(T);

            return ProviderSearchAsync(providerType, query, ct);
        }

        private async Task ProviderSearchAsync(Type providerType, string query, CancellationToken ct)
        {
            try
            {
                ISearchProvider provider = (ISearchProvider)Activator.CreateInstance(providerType);

                provider.ItemFound += Provider_ItemFound;
                try
                {
                    await provider.SearchAsync(query, ct);

                    // Artificial delay to give the user time to react
                    // (We may not need to start searching the next provider yet)
                    await Task.Delay(500, ct);
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
                numItemsFound = 0;

                // Query each provider sequentially
                // NOTE: In the future we may want to parallelize this with Task.WhenAll(...)
                foreach (var provider in this.EnabledProviders)
                {
                    OnStatusChanged($"Searching {provider.Name}");
                    await ProviderSearchAsync(provider, query, ct);
                }

                if (numItemsFound == 0)
                    OnStatusChanged("No results found");
                else
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
            numItemsFound++;
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

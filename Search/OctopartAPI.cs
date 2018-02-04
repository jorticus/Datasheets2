using Datasheets2.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Datasheets2.Search
{
    public class OctopartAPI : ISearchProvider
    {
        public string Name { get { return "Octopart.com"; } }

        private const string API_ENDPOINT = "https://octopart.com/api/v3";

        protected class OctopartSearchResult : SearchResult
        {
            public OctopartSearchResult(OctopartAPI provider)
                : base(provider)
            { }
        }

        public async Task SearchAsync(string query, CancellationToken ct)
        {
            var apiKey = App.Current.ApiKeys.GetKey("Octopart");

            // https://octopart.com/api/docs/v3/overview#datasheets
            query = Uri.EscapeDataString(query);
            var uri = new Uri(API_ENDPOINT + $"/parts/search?apikey={apiKey.SecretKey}&q={query}&start=0&limit=5&include=datasheets");

            var json = await WebUtil.RequestJsonAsync<Newtonsoft.Json.Linq.JObject>(uri, ct);

            int hits = json.Value<int>("hits");
            if (hits <= 0)
                return; // No results

            var results = json["results"];

            //foreach (var result in results)
            var result = results?.First;
            {
                var description = result.Value<string>("snippet");
                var item_ob = result["item"];
                if (item_ob != null)
                {
                    var partName = item_ob.Value<string>("mpn");
                    var url = item_ob.Value<string>("octopart_url");
                    var itemUri = new Uri(url, UriKind.Absolute);

                    // Manufacturer info
                    var manufacturer_ob = item_ob["manufacturer"];
                    var manufacturer = manufacturer_ob?.Value<string>("name");
                    var manufacturerUrl = manufacturer_ob?.Value<string>("homepage_url");

                    // Datasheets from various sources
                    var datasheets = item_ob["datasheets"];
                    if (datasheets == null)
                        return;

                    foreach (var datasheet in datasheets)
                    {
                        var dsMimeType = datasheet.Value<string>("mimetype");
                        var dsUrl = datasheet.Value<string>("url");
                        var dsSize = datasheet.Value<JToken>("metadata")?.Value<int>("size_bytes");
                        var dsPages = datasheet.Value<JToken>("metadata")?.Value<int>("num_pages");
                        var dsSource = datasheet.Value<JToken>("attribution")?.Value<JToken>("sources")?.First;
                        var dsSourceName = dsSource?.Value<string>("name");

                        var dsUri = new Uri(dsUrl, UriKind.Absolute);

                        //if (dsMimeType != "application/pdf")
                        //    throw new Exception($"Unsupported datasheet type {dsMimeType}");

                        Debug.WriteLine($"Octopart: {partName}, {url}, {manufacturer}, {dsUri}, {dsSourceName}");

                        OnItemFound(new OctopartSearchResult(this)
                        {
                            WebpageUrl = itemUri,
                            DatasheetUrl = dsUri,
                            PartName = partName,
                            DatasheetSource = dsSourceName,
                            DatasheetFileSize = dsSize,
                            DatasheetPages = dsPages,
                            Description = description,
                            Manufacturer = manufacturer
                        });
                    }
                }
            }

            return;
        }

        public event EventHandler<ItemFoundEventArgs> ItemFound;

        private void OnItemFound(SearchResult item)
        {
            ItemFound?.Invoke(this, new ItemFoundEventArgs(item));
        }
    }
}

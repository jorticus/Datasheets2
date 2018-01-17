using Datasheets2.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datasheets2.Models
{
    public class WebSearchItem : ISearchResult
    {
        public WebSearchItem(Uri url = null, string label = null, string description = null)
        {
            this.DatasheetUrl = url;
            this.PartName = label;
            this.Description = description;
        }

        public string PartName { get; set; }
        public Uri DatasheetUrl { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datasheets2.Models
{
    public class WebSearchItem
    {
        public WebSearchItem(Uri url = null, string label = null, string description = null)
        {
            this.Url = url;
            this.Label = label;
            this.Description = description;
        }

        public string Label { get; set; }
        public Uri Url { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }

    }
}

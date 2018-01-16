using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Datasheets2.Models
{
    public class Tag
    {
        public Tag(string label, Color? color = null)
        {
            this.Label = label;
            this.Color = Colors.Black;// color ?? Color.Black;
        }

        public string Label { get; set; }
        public Color Color { get; set; }
    }
}

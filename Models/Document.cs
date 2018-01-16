using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datasheets2.Models
{
    public class Document : Item
    {
        public Document(string filePath, string label = null)
            : base(filePath, label)
        {
            //this.Tags = new List<Tag>
            //{
            //    new Tag("test", System.Windows.Media.Colors.DarkGreen)
            //};
        }
    }
}

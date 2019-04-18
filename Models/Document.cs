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
        public Document(string filePath,IItem parent, string label = null)
            : base(filePath, parent, label)
        {
            //this.Tags = new List<Tag>
            //{
            //    new Tag("test", System.Windows.Media.Colors.DarkGreen)
            //};
        }

        public override void Rename(string newName)
        {
            string currExt = System.IO.Path.GetExtension(FilePath);

            base.Rename(newName);

            // Reset icon if filetype has changed
            string newExt = System.IO.Path.GetExtension(newName);
            if (currExt != newExt)
            {
                ResetIcon();
            }
        }
    }
}

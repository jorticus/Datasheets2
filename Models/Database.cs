using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datasheets2.Models
{
    public class Database : INotifyPropertyChanged
    {
        protected Folder root;

        public Database()
        {
            
        }

        public async Task LoadAsync(string path)
        {
            Root = new Folder(path);
            await Root.LoadAsync();
            OnPropertyChanged("Items");
        }

        public Task RefreshAsync()
        {
            return Root.LoadAsync();
        }

        public async Task SaveAsync()
        {
           
        }

        public Folder Root
        {
            get { return root; }
            set { root = value; ApplyFilter(); OnPropertyChanged("Root"); }
        }

        private string _filter;
        public string Filter
        {
            get { return _filter; }
            set { _filter = value; ApplyFilter(); OnPropertyChanged("Filter"); }
        }

        public IEnumerable<IItem> Items
        {
            //get { return GetFilteredItems(); }
            get { return Root?.Items; }
        }

        protected void ApplyFilter()
        {
            if (Root != null)
                Root.Filter = this.Filter;

            // Force property update of Items
            OnPropertyChanged("Items");
        }

        //protected IEnumerable<IItem> GetFilteredItems()
        //{
        //    if (string.IsNullOrWhiteSpace(_filter))
        //    {
        //        return Root?.Items;
        //    }
        //    else
        //    {
        //        // Present items as a flat list with no folders
        //        //return Root?.GetFilteredItems(_filter, flatten: true);

        //        // Present items as a heirarchical list, with empty leaf folders removed
        //        //return Root?.GetFilteredItems(_filter, flatten: false);
        //    }
        //}

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}

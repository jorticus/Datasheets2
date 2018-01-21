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
        /*#region Singleton

        public static Database Instance { get { return GetInstance(); } }
        protected static Database _instance = null;

        protected static Database GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Database();
            }
            return _instance;
        }

        #endregion*/

        protected Folder root;
        //protected List<Document> documents;

        public Database()
        {
            
        }

        public async Task LoadAsync(string path)
        {
            Root = new Folder(path);
            await Root.LoadAsync();
            //Documents.Load();
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
            get { return GetFilteredItems(); }
        }

        protected void ApplyFilter()
        {
            // Force property update of Items
            OnPropertyChanged("Items");
        }

        protected IEnumerable<IItem> GetFilteredItems()
        {
            if (string.IsNullOrWhiteSpace(_filter))
            {
                return Root?.Items;
            }
            else
            {
                return Root?.GetFilteredItems(_filter, flatten: true);
            }
        }

        public void AddItem(IItem item)
        {
            //RefreshAsync();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}

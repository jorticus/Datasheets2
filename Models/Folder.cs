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
    public class Folder : Item
    {
        private ObservableCollection<IItem> _items;

        /// <summary>
        /// An observable collection of child items
        /// </summary>
        public ObservableCollection<IItem> Items
        {
            get { return _items; }
            set { _items = value; OnPropertyChanged("Items"); }
        }

        /// <summary>
        /// A filtered view of Items. Not observable.
        /// </summary>
        public IEnumerable<Folder> Folders { get { return _items.Where(x => x is Folder).Select(x => (Folder)x); } }

        /// <summary>
        /// A filtered view of Items. Not observable.
        /// </summary>
        public IEnumerable<Document> Documents { get { return _items.Where(x => x is Document).Select(x => (Document)x); } }


        /// <summary>
        /// Return a list of documents filtered by the specified filter.
        /// </summary>
        /// <param name="filter">Filtering string</param>
        /// <returns></returns>
        private IEnumerable<IItem> GetFilteredDocuments(string filter)
        {
            return Documents.Where(x => x.Filter(filter)).Select(x => (IItem)x);
        }

        /// <summary>
        /// Return a flattened list of filtered documents from this folder's subfolders
        /// </summary>
        /// <param name="filter">Filtering string</param>
        /// <returns></returns>
        private IEnumerable<IItem> GetFilteredFoldersFlattened(string filter)
        {
            return Folders.SelectMany(x => x.GetFilteredItems(filter));
        }

        /// <summary>
        /// Return a list of virtualized Folders with the specified filter applied.
        /// Folders that result in empty contents are omitted.
        /// Not observable.
        /// </summary>
        /// <param name="filter">Filtering string</param>
        /// <returns></returns>
        private IEnumerable<IItem> GetFilteredFoldersVirtual(string filter)
        {
            return Folders.Select(x => {
                var filtereddocs = x.GetFilteredItems(filter);
                return (filtereddocs.Count() > 0) ? new Folder(x, filtereddocs) : null;
            }).Where(f => f != null);
        }

        /// <summary>
        /// Get a new list of IItems filtered with the specified filtering string
        /// </summary>
        /// <param name="filter">Filtering string</param>
        /// <param name="flatten">
        /// if true, a flattened list of Documents are returned. 
        /// if false, a list of Documents & virtualized Folders will be returned.
        /// </param>
        /// <returns></returns>
        public IEnumerable<IItem> GetFilteredItems(string filter, bool flatten = false)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return _items;
            }
            else
            {
                var subfolders = (flatten) ?
                     GetFilteredFoldersFlattened(filter) :
                     GetFilteredFoldersVirtual(filter);

                var filtereddocs = GetFilteredDocuments(filter);
                return subfolders.Concat(filtereddocs);
            }
        }

        public Folder(string filePath, ObservableCollection<IItem> items = null) :
            base(filePath)
        {
            this.Items = items ?? new ObservableCollection<IItem>();
        }

        /// <summary>
        /// Create a virtualized folder
        /// </summary>
        /// <remarks>
        /// Calling Load on this is not valid.
        /// </remarks>
        /// <param name="other"></param>
        /// <param name="items"></param>
        public Folder(Folder other, IEnumerable<IItem> items)
            : base(other.FilePath, label:other.Label)
        {
            this.Items = new ObservableCollection<IItem>(items);
        }

        public static IEnumerable<IItem> ScanPath(string path)
        {
            if (!Directory.Exists(path))
                throw new FileNotFoundException($"Could not find path '{path}'");

            var directories = Directory.GetDirectories(path);
            foreach (var dirPath in directories)
            {
                var folder = new Folder(dirPath);
                folder.Load();
                yield return folder;
            }

            var files = Directory.GetFiles(path);
            foreach (var filePath in files)
            {
                var doc = new Document(filePath);
                yield return doc;
            }
        }

        public void Load()
        {
            foreach (var item in ScanPath(FilePath))
            {
                Items.Add(item);
            }
        }

        private static Task<IEnumerable<string>> GetDirectoriesAsync(string path)
        {
            // Hacky way of creating async directory query using the Thread Pool
            return Task.Run(() =>
            {
                return (IEnumerable<string>)Directory.GetDirectories(path).ToList();
            });
        }

        private static Task<IEnumerable<string>> GetFilesAsync(string path)
        {
            return Task.Run(() =>
            {
                return (IEnumerable<string>)Directory.GetFiles(path).ToList();
            });
        }

        /// <summary>
        /// Populate Items from the file system (specified by the folder's path)
        /// </summary>
        public async Task LoadAsync()
        {
            // Do not load if we already have items
            // (TODO: What if we want to refresh?)
            if (Items.Count > 0)
                return;

            var path = this.FilePath;
            if (!Directory.Exists(path))
                throw new FileNotFoundException($"Could not find path '{path}'");
            
            foreach (var dirPath in await GetDirectoriesAsync(path))
            {
                var folder = new Folder(dirPath);
                await folder.LoadAsync();
                Items.Add(folder);
                await Task.Yield();
            }

            int i = 0;
            foreach (var filePath in await GetFilesAsync(path))
            {
                var doc = new Document(filePath);
                Items.Add(doc);

                // Allow UI tasks to run occasionally
                if (++i % 20 == 0)
                {
                    await Task.Yield();
                    await Task.Delay(1);
                }
            }
        }
    }
}

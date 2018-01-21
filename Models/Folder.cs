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
        /// Insert item in the correct order.
        /// Use this instead of Items.Add() to preserve order
        /// </summary>
        /// <param name="item">A new item to insert. Should not exist in the list</param>
        public void InsertItem(IItem item)
        {
            var comparer = Comparer<string>.Default;

            int i;
            for (i = 0; i < this.Items.Count; i++)
            {
                var dbItem = Items[i];
                bool dbIsFolder = (dbItem is Folder);
                bool itemIsFolder = (item is Folder);

                // Files come after folders, so skip until we reach the files
                if (!itemIsFolder && dbIsFolder)
                    continue;

                // Iterate through items until we find the point that we want to insert
                // (Assumes Items is already sorted alphabetically)
                if (comparer.Compare(dbItem.Label, item.Label) >= 0)
                    break;

                // Put folder at end of folders, but before files
                if (itemIsFolder && !dbIsFolder)
                    break;
            }

            this.Items.Insert(i, item);
        }

        /// <summary>
        /// Populate Items from the file system (specified by the folder's path)
        /// </summary>
        public async Task LoadAsync()
        {
            bool refresh = (Items.Count > 0);

            if (!Directory.Exists(this.FilePath))
                throw new FileNotFoundException($"Could not find path '{FilePath}'");

            await LoadFoldersRecursiveAsync();
            await LoadDocumentsAsync();
        }

        private async Task LoadFoldersRecursiveAsync()
        {
            var path = this.FilePath;

            var dbFolders = this.Folders.ToList();
            var dbFolderPaths = dbFolders.Select(d => d.FilePath);

            var fsFolderPaths = (await GetDirectoriesAsync(path))
                .OrderBy(p => System.IO.Path.GetDirectoryName(p))
                .ToList();

            // Remove folders that no longer exist on the filesystem
            var deletedFolders = dbFolderPaths.Except(fsFolderPaths);
            foreach (var dirPath in deletedFolders)
            {
                Folder folder = dbFolders.First(d => d.FilePath == dirPath);
                this.Items.Remove(folder);
                await Task.Yield();
            }

            // Add new folders
            var addedFolders = fsFolderPaths.Except(dbFolderPaths);
            foreach (var dirPath in addedFolders)
            {
                var folder = new Folder(dirPath);
                this.InsertItem(folder);
                await Task.Yield();
            }

            // Recurse into each folder, even if folder is already loaded (in case contents have changed)
            foreach (var folder in this.Folders)
            {
                await folder.LoadAsync();
            }
        }

        private async Task LoadDocumentsAsync()
        {
            var path = this.FilePath;

            int i;
            var dbDocuments = this.Documents.ToList();
            var dbDocumentPaths = dbDocuments.Select(d => d.FilePath);
            var fsDocumentPaths = await GetFilesAsync(path);

            // Remove documents from the list that no longer exist on the filesystem
            i = 0;
            var deletedDocuments = dbDocumentPaths.Except(fsDocumentPaths);
            foreach (var filePath in deletedDocuments)
            {
                var item = this.Documents.First(d => d.FilePath == filePath); // TODO: Potentially O(n2)
                this.Items.Remove(item);

                // Allow UI tasks to run occasionally
                if (++i % 20 == 0)
                {
                    await Task.Yield();
                    await Task.Delay(1);
                }
            }

            // Add documents which have been added to the filesystem but are not present in the list
            i = 0;
            var addedDocuments = fsDocumentPaths.Except(dbDocumentPaths);
            foreach (var filePath in addedDocuments)
            {
                var doc = new Document(filePath);
                this.InsertItem(doc);

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

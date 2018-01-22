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
        private FileSystemWatcher fsWatcher;

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
            this.fsWatcher = null;
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

        protected static IEnumerable<IItem> ScanPath(string path)
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

        protected void Load()
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

            InitFileSystemWatcher();
        }

        private void InitFileSystemWatcher()
        {
            //if (fsWatcher != null)
            //{
            //    // Unload original
            //    fsWatcher.EnableRaisingEvents = false;
            //    fsWatcher.Created -= FsWatcher_Created;
            //    fsWatcher.Changed -= FsWatcher_Changed;
            //    fsWatcher.Renamed -= FsWatcher_Renamed;
            //    fsWatcher.Deleted -= FsWatcher_Deleted;
            //    fsWatcher.Dispose();
            //}

            // NOTE1: If Folder is re-named, we must update the FSwatcher path or things get screwey.
            // (Watcher will keep watching the dir, but report the old path)
            // NOTE2: Nested filesystemwatchers seems to prevent the root-level watcher from being deleted.
            // A better method would be to just have a single watcher that keeps track of subfolders.
            if (fsWatcher == null)
            {
                fsWatcher = new FileSystemWatcher(this.FilePath);
                //fsWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
                //fsWatcher.Filter = "*.*";
                fsWatcher.EnableRaisingEvents = true;
                fsWatcher.Created += FsWatcher_Created;
                fsWatcher.Changed += FsWatcher_Changed;
                fsWatcher.Renamed += FsWatcher_Renamed;
                fsWatcher.Deleted += FsWatcher_Deleted;
            }
        }

        private void FsWatcher_Created(object sender, FileSystemEventArgs e)
        {
            IItem item;
            var newPath = e.FullPath;
            if (System.IO.File.Exists(newPath))
            {
                item = new Document(newPath);
            }
            else
            {
                item = new Folder(newPath);
            }

            App.Current.Dispatcher.Invoke(async () =>
            {
                InsertItem(item);

                if (item is Folder)
                    await (item as Folder).LoadAsync();
            });
        }

        private void FsWatcher_Changed(object sender, FileSystemEventArgs e)
        {

        }

        private void FsWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            var item = Items.FirstOrDefault(i => i.FilePath == e.OldFullPath);
            if (item != null)
            {
                string oldDirPath = System.IO.Path.GetDirectoryName(e.OldFullPath);
                string dirPath = System.IO.Path.GetDirectoryName(e.FullPath);
                if (oldDirPath != dirPath)
                {
                    // This is actually a move.
                    // For now, remove and re-insert.
                    // TODO: Optimization - carry existing subitems along with the new folder
                    this.Items.Remove(item);
                    FsWatcher_Created(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, e.FullPath, e.Name));
                }
                else
                {
                    item.Rename(e.Name);
                }
            }
            else
            {
                // Hmm, a file was re-named that we don't know about.
                // Add it.
                FsWatcher_Created(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, e.FullPath, e.Name));
            }
        }

        private void FsWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var item = Items.FirstOrDefault(i => i.FilePath == e.FullPath);
            if (item != null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    this.Items.Remove(item);
                });
            }
        }

        public override void Rename(string newName)
        {
            base.Rename(newName);

            // Update FS Watcher when path changes
            if (fsWatcher != null)
                fsWatcher.Path = this.FilePath;
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

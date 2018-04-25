using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Datasheets2.Models
{
    public interface IItem
    {
        string FilePath { get; }

        string Label { get; set; }

        IList<Tag> Tags { get; set; }

        ImageSource Icon { get; }

        void OpenItem();

        void Rename(string newName);
    }

    public class Item : IItem, INotifyPropertyChanged
    {
        private bool _isSelected = false;
        private bool _isVisible = true;
        private string filePath;

        public Item(string filePath, string label = null)
        {
            this._label = label ?? System.IO.Path.GetFileNameWithoutExtension(filePath);
            this.filePath = filePath;
        }

        public string FilePath { get { return filePath; } }

        private string _label;
        public string Label
        {
            get { return _label; }
            set { _label = value; OnPropertyChanged("Label"); }
        }

        private IList<Tag> _tags;
        public IList<Tag> Tags
        {
            get { return _tags; }
            set { _tags = value; OnPropertyChanged("Tags"); }
        }

        public ImageSource Icon { get { return GetIconImageSource(); } }

        /// <summary>
        /// Bound to the TreeViewItem's IsSelected property
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set { bool x = _isSelected; _isSelected = value; if (x != value) { OnPropertyChanged("IsSelected"); } }
        }

        /// <summary>
        /// If false, this item will be filtered out and not displayed in the UI
        /// </summary>
        public bool IsVisible
        {
            get { return _isVisible; }
            set { bool x = _isVisible; _isVisible = value; if (x != value) { OnPropertyChanged("IsVisible"); } }
        }

        public bool FilterResult(string filter)
        {
            return Label.ToLowerInvariant().Contains(filter.ToLowerInvariant());
        }

        public override string ToString()
        {
            return Label;
        }

        protected ImageSource GetIconImageSource()
        {
            // Folders are easy
            if (this is Folder)
                return IconCache.GetIconForFolder();

            // Look up icon in cache
            ImageSource source = IconCache.GetIconForFile(this.FilePath, onLoaded: () =>
            {
                // This will only get called if the icon needs to be loaded

                // Force a re-fetch of the Icon property. The icon should now be loaded in the cache.
                App.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(this.Icon));
                });
            });

            // This may be null if icon is not yet available.
            return source; 
        }

        public void OpenItem()
        {
            try
            {
                ShellOperation.ShellExecute(this.FilePath);
            }
            catch (Exception e)
            {
                //((App)App.Current)
            }
        }

        public virtual void Rename(string newName)
        {
            string currPath = System.IO.Path.GetDirectoryName(filePath);
            string newPath = System.IO.Path.Combine(currPath, newName);

            this.filePath = newPath;
            OnPropertyChanged("FilePath");
            OnPropertyChanged("Label");
        }

        public ICommand OpenCommand { get { return new RelayCommand((o) => {
            OpenItem(); }); } }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

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

            //var iconLoadTask = Task.Factory.StartNew(async () => {
            //    await LoadImageSourceAsync();
            //});

            ResetIcon();
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

        private Lazy<ImageSource> _lazyicon;
        public ImageSource Icon { get { return _lazyicon.Value; } }

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

        protected void ResetIcon()
        {
            this._lazyicon = new Lazy<ImageSource>(GetIconImageSource);
            OnPropertyChanged("Icon");
        }

        //private ImageSource _icon;
        //public ImageSource Icon
        //{
        //    get { return _icon; }
        //    private set { _icon = value; OnPropertyChanged("Icon"); }
        //}

        public bool FilterResult(string filter)
        {
            return Label.ToLowerInvariant().Contains(filter.ToLowerInvariant());
        }

        public override string ToString()
        {
            return Label;
        }

        //protected async Task LoadImageSourceAsync()
        //{
        //    // Retrieve the icon for the file/folder represented by this Item
        //    // NOTE: Default (path==null) is the Folder icon.
        //    string path = this.FilePath;
        //    //return System.Drawing.Icon.ExtractAssociatedIcon(path);
        //    //var icon = IconUtil.GetSmallIconForExtension(path);
        //    var icon = await IconUtil.GetIconForPathAsync(path, IconUtil.IconSize.SmallIcon);

        //    if (icon != null)
        //    {
        //        // Convert to ImageSource so we can bind it
        //        icon = new Icon(icon, 16, 16);

        //        // You must create the ImageSource from within the UI thread
        //        App.Current.Dispatcher.Invoke(() =>
        //        {
        //            this.Icon = Imaging.CreateBitmapSourceFromHIcon(
        //                icon.Handle,
        //                //System.Windows.Int32Rect.Empty,
        //                new System.Windows.Int32Rect(0, 0, 16, 16),
        //                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        //        });
        //    }
        //}

        protected ImageSource GetIconImageSource()
        {
            string path = this.FilePath;

            // Optimization: It helps if we know if the path is a directory or a file.
            IconUtil.PathType? pathType = null;
            if (this is Folder)
                pathType = IconUtil.PathType.Directory;
            else if (this is Document)
                pathType = IconUtil.PathType.File;

            // Retrieve the icon for the file/folder represented by this Item
            // NOTE: Default (path==null) is the Folder icon.
            //Task.Factory.StartNew<Task<ImageSource>>(IconUtil.GetIconImageSourceForPathAsync(path, pathType), TaskCreationOptions.LongRunning);
            return IconUtil.GetIconImageSourceForPath(path, pathType);
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

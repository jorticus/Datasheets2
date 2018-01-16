using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
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
    }

    public class Item : IItem, INotifyPropertyChanged
    {
        private string filePath;

        public Item(string filePath, string label = null)
        {
            // Automatic label
            if (label == null && filePath != null)
                label = System.IO.Path.GetFileNameWithoutExtension(filePath);

            this.filePath = filePath;
            this._label = label;

            //var iconLoadTask = Task.Factory.StartNew(async () => {
            //    await LoadImageSourceAsync();
            //});


            this._lazyicon = new Lazy<ImageSource>(GetIconImageSource);
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

        //private ImageSource _icon;
        //public ImageSource Icon
        //{
        //    get { return _icon; }
        //    private set { _icon = value; OnPropertyChanged("Icon"); }
        //}

        public bool Filter(string filter)
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
            var icon = IconUtil.GetIconForPathAsync(path, IconUtil.IconSize.SmallIcon, pathType).Result; // Blocking

            //if (pathType != IconUtil.PathType.File)
            //    return null;
            //var icon = System.Drawing.Icon.ExtractAssociatedIcon(path); // Only returns 32x32 icon

            if (icon != null)
            {
                // Convert to ImageSource so we can bind it
                return Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    //System.Windows.Int32Rect.Empty,
                    new System.Windows.Int32Rect(0, 0, 16, 16),
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            return null;
        }

        public void OpenItem()
        {
            //System.Windows.MessageBox.Show($"Open: {this}");
            Process.Start(this.FilePath);
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

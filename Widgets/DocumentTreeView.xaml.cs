using Datasheets2.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Datasheets2.Widgets
{
    /// <summary>
    /// Interaction logic for DocumentTreeView.xaml
    /// </summary>
    public partial class DocumentTreeView : UserControl
    {
        //private DispatcherTimer treeScrollTimer;

        public DocumentTreeView()
        {
            InitializeComponent();


            //treeScrollTimer = new DispatcherTimer();
            //treeScrollTimer.Interval = TimeSpan.FromMilliseconds(500);
            //treeScrollTimer.Tick += scrollTimer_Callback;

        }


        // TODO: This doesn't work and I don't know why
        //public static readonly DependencyProperty ItemsSourceProperty =
        //    DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<IItem>), typeof(DocumentTreeView),
        //        new PropertyMetadata(null));

        //public ObservableCollection<IItem> ItemsSource
        //{
        //    get { return (ObservableCollection<IItem>)GetValue(ItemsSourceProperty); }
        //    set { SetValue(ItemsSourceProperty, value); }
        //}


        public static readonly DependencyProperty DatabaseProperty =
            DependencyProperty.Register("Database", typeof(Database), typeof(DocumentTreeView),
                new PropertyMetadata(null));

        public Database Database
        {
            get { return (Database)GetValue(DatabaseProperty); }
            set { SetValue(DatabaseProperty, value); }
        }

        public TreeView TreeView { get { return tree; } }

        public IItem SelectedItem { get { return (tree.SelectedItem as IItem); } }

        public new void Focus()
        {
            tree.Focus();
        }

        public void SelectFirst()
        {
            //tree.Focus();
            
            var firstItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
            if (firstItem != null)
                firstItem.IsSelected = true;
        }

        public void SelectFirstDocument()
        {
            var treeViewItem = GetFirstDocument(tree);
            if (treeViewItem != null)
                treeViewItem.IsSelected = true;
        }

        private TreeViewItem GetFirstDocument(ItemsControl tree)
        {
            for (int i = 0; i < tree.ItemContainerGenerator.Items.Count; i++)
            {
                var treeViewItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(i);
                if (treeViewItem != null)
                {
                    if (treeViewItem.DataContext is Models.Document)
                    {
                        return treeViewItem;
                    }
                    else
                    {
                        // This is a Folder, recurse into it (depth-first)
                        treeViewItem = GetFirstDocument(treeViewItem);
                        if (treeViewItem != null)
                        {
                            // If this is not null, it's guaranteed to be a Document
                            return treeViewItem;
                        }
                    }
                }
            }

            return null;
        }

        public void UnfocusTree()
        {
            var currentItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(tree.SelectedItem);
            if (currentItem != null)
            {
                currentItem.IsSelected = false;
            }
        }

        public bool IsFirstItemSelected
        {
            get
            {
                var currentItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(tree.SelectedItem);
                var firstItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
                return (currentItem == firstItem);
            }
        }

        private void tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Handle double-click on tree view item
                var treeViewItem = (sender as TreeViewItem);
                if (treeViewItem != null && treeViewItem.IsSelected)
                {
                    var item = treeViewItem.DataContext as IItem;
                    if (item != null && item is Document)
                        item.OpenItem();

                    // Prevent triggering child items
                    return;
                }
            }
        }

        private void tree_KeyUp(object sender, KeyEventArgs e)
        {
            // Ignore any key presses if tree doesn't have focus (eg. context menu is open)
            // TODO: BUG: This is always false??
            //if (!(sender as TreeView).IsFocused)
            //    return;

            // Handle enter on tree view item
            if (e.Key == Key.Enter)
            {
                var item = this.SelectedItem;
                if (item != null)
                    item.OpenItem();
            }
        }


        //private void scrollTimer_Callback(object sender, EventArgs e)
        //{
        //    var hit = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(tree.SelectedItem);
        //    if (hit != null)
        //    {
        //        int idx = tree.ItemContainerGenerator.IndexFromContainer(hit);
        //        if (idx > 0)
        //            hit = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(idx - 1);

        //        if (hit != null)
        //            hit.IsSelected = true;
        //    }
        //}

        private TreeViewItem GetTreeViewItemAtPoint(Point pos)
        {

            // Find TreeViewItem under the cursor
            var hit = tree.InputHitTest(pos) as DependencyObject;
            while (!(hit is TreeViewItem) && hit != null)
            {
                hit = VisualTreeHelper.GetParent(hit);
            }
            return (TreeViewItem)hit;
        }

        private void tree_MouseMove(object sender, MouseEventArgs e)
        {
            // Simulate behaviour of old Datasheets app where holding mouse button 
            // in the list will keep selecting the item under the mouse as it moves.
            // TODO: Doesn't work if mouse leaves the treeview area
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(tree);

                var hit = GetTreeViewItemAtPoint(pos);

                //if (pos.Y < 6)
                //{
                //    treeScrollTimer.Start();
                //}
                //else if (pos.Y > tree.Height - 6)
                //{
                //    // TODO
                //}
                //else
                //{
                //    treeScrollTimer.Stop();
                //}

                if (hit != null)
                {
                    ((TreeViewItem)hit).IsSelected = true;
                }
            }
        }

        private void tree_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //if (e.LeftButton == MouseButtonState.Released)
            //{
            //    treeScrollTimer.Stop();
            //}
            //Mouse.Capture(null);
        }

        private void tree_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //((IInputElement)sender).CaptureMouse();
        }

        #region Context Menu Actions

        #endregion

        private void miOpenFolder_Activate(object sender, RoutedEventArgs e)
        {
            // Open the documents library in explorer
            ShellOperation.ShellOpenFolder(App.Current.DocumentsDir);
        }

        private void TreeViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Force selection of item on right click, before context menu is shown
            //var treeViewItem = (sender as TreeViewItem);
            var pos = e.GetPosition(tree);
            var treeViewItem = GetTreeViewItemAtPoint(pos);
            treeViewItem.IsSelected = true;
        }

        private void TreeViewItem_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void miOpenItem_Activate(object sender, RoutedEventArgs e)
        {
            var item = tree.SelectedItem as IItem;
            if (item != null)
            {
                item.OpenItem();
            }
        }

        private bool _dragInProgress = false;
        private TreeViewItem _selectionBeforeDrag = null;

        private Tuple<DragDropEffects, string[], Uri> tree_DragOperation(object sender, DragEventArgs e)
        {
            // Default: Deny drop
            DragDropEffects operation = DragDropEffects.None;
            bool supported = false;
            //string desc = "";

            var filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (filenames != null && filenames.Length != 0)
            {
                //if (SUPPORTED_FILE_EXTS != null)
                //{
                //    // Validate that all files match valid extensions
                //    var filetypes = filenames.Select(fname => System.IO.Path.GetExtension(fname).ToLowerInvariant());
                //    supported = filetypes.All(ext => SUPPORTED_FILE_EXTS.Contains(ext));
                //}

                //desc = (filenames.Length == 1) ?
                //    $"{System.IO.Path.GetFileName(filenames[0])}" :
                //    //"1 file" :
                //    $"{filenames.Length} files";

                supported = true;

                // Default operation:
                operation = DragDropEffects.Copy;
            }

            // No files dropped, maybe it was a URL?
            Uri uri = null;
            if (!supported)
            {
                //if (e.Data.GetData("FileGroupDescriptor") != null && e.Data.GetData("FileContents") != null)
                //{
                //    supported = true;

                //    // Default operation:
                //    operation = DragDropEffects.Link;
                //}

                var text = (string)e.Data.GetData(DataFormats.Text, true);

                if (Uri.TryCreate(text, UriKind.Absolute, out uri))
                {
                    supported = true;

                    // Default operation:
                    operation = DragDropEffects.Link;
                }
            }


            // We support this operation, figure out if it's a copy/move/link operation
            if (supported)
            {
                // Standard windows override keys:
                // CTRL  : Copy
                // SHIFT : Move
                // ALT   : Link

                bool moveAllowed = ((e.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move);
                bool copyAllowed = ((e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy);
                bool linkAllowed = false;// ((e.AllowedEffects & DragDropEffects.Link) == DragDropEffects.Link);

                bool ctrlPressed = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;
                bool shiftPressed = (e.KeyStates & DragDropKeyStates.ShiftKey) == DragDropKeyStates.ShiftKey;
                bool altPressed = (e.KeyStates & DragDropKeyStates.AltKey) == DragDropKeyStates.AltKey;

                // Override operation if modifier key pressed
                if (shiftPressed && moveAllowed)
                {
                    operation = DragDropEffects.Move;
                }
                else if (ctrlPressed && copyAllowed)
                {
                    operation = DragDropEffects.Copy;
                }
                else if (altPressed && linkAllowed)
                {
                    operation = DragDropEffects.Link;
                }

                //switch (operation)
                //{
                //    case DragDropEffects.Copy:
                //        dropText.Text = $"Copy {desc} to {App.Current.DocumentsDir}";
                //        break;

                //    case DragDropEffects.Move:
                //        dropText.Text = $"Move {desc} to {App.Current.DocumentsDir}";
                //        break;

                //    case DragDropEffects.Link:
                //        dropText.Text = $"Create link to {desc}";
                //        break;
                //}
            }

            return new Tuple<DragDropEffects, string[], Uri>(operation, filenames, uri);
        }

        private void tree_DragOver(object sender, DragEventArgs e)
        {
            // Capture selected item before drag was initiated
            if (!_dragInProgress && _selectionBeforeDrag != null)
            {
                _selectionBeforeDrag = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(tree.SelectedItem);
            }
            _dragInProgress = true;

            try
            {
                // Determine if drag is allowed
                var result = tree_DragOperation(sender, e);
                e.Effects = result.Item1;

                // Show hover effects if drag is allowed
                bool showOverlay = (e.Effects != DragDropEffects.None);
                if (showOverlay)
                {
                    var pos = e.GetPosition(tree);
                    var treeViewItem = GetTreeViewItemAtPoint(pos);
                    if (treeViewItem != null && treeViewItem.DataContext is Folder)
                    {
                        // Select/Highlight folders
                        treeViewItem.IsSelected = true;
                    }
                    else
                    {
                        // Don't select anything if dragging over a file or empty space
                        var currentItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(tree.SelectedItem);
                        if (currentItem != null)
                            currentItem.IsSelected = false;
                    }
                }

                if (showOverlay != dropOverlay.IsEnabled)
                    dropOverlay.IsEnabled = showOverlay;
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void tree_DragLeave(object sender, DragEventArgs e)
        {
            _dragInProgress = false;

            // https://stackoverflow.com/questions/5447301/wpf-drag-drop-when-does-dragleave-fire
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_dragInProgress == false)
                    tree_DragLeaveForReal(sender, e);
            }));
        }

        private void tree_DragLeaveForReal(object sender, DragEventArgs e)
        {
            // Clear the current "selection" (selected by drag)
            var currentItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(tree.SelectedItem);
            if (currentItem != null)
                currentItem.IsSelected = false;

            // Restore selection before drag
            if (_selectionBeforeDrag != null)
            {
                _selectionBeforeDrag.IsSelected = true;
                _selectionBeforeDrag = null;
            }

            // Hide overlay
            if (dropOverlay.IsEnabled)
                dropOverlay.IsEnabled = false;

            e.Handled = true;
        }

        private async void tree_Drop(object sender, DragEventArgs e)
        {
            // Determine the requested operation
            var result = tree_DragOperation(sender, e);
            var operation = result.Item1;
            var srcfiles = result.Item2;
            var srcuri = result.Item3;

            // Nothing dropped
            if ((srcfiles == null || srcfiles.Length == 0) && (srcuri == null))
                return;

            // Default to the library root
            var destdir = App.Current.DocumentsDir;

            // If dropped onto a folder, that's where the file should go
            var currentItem = (IItem)tree.SelectedItem;
            if (currentItem != null)
            {
                if (currentItem is Folder)
                {
                    destdir = currentItem.FilePath;

                    // Sanity check - don't want the file to end up somewhere outside the library!
                    if (!System.IO.Path.GetFullPath(destdir).StartsWith(App.Current.DocumentsDir))
                        throw new InvalidOperationException($"Unexpected destination: {destdir}");
                }
            }

            // Otherwise this doesn't get called if you drop
            tree_DragLeaveForReal(sender, e);

            if (srcfiles != null && srcfiles.Length > 0)
            {
                switch (operation)
                {
                    case DragDropEffects.Copy:
                        await ShellOperation.SHFileOperationAsync(ShellOperation.FileOperation.Copy, srcfiles.ToArray(), destdir);
                        //await Database.RefreshAsync();
                        break;

                    case DragDropEffects.Move:
                        await ShellOperation.SHFileOperationAsync(ShellOperation.FileOperation.Move, srcfiles.ToArray(), destdir);
                        //await Database.RefreshAsync();
                        break;

                    default:
                        // Not supported
                        throw new NotImplementedException($"{operation} operation not supported");
                }
            }
            else if (srcuri != null)
            {
                if (operation == DragDropEffects.Link)
                {
                    if (e.Data.GetData("FileGroupDescriptor") != null)
                    {
                        ShellOperation.ExtractUrlFileFromDrop(e, destdir);
                    }
                    else
                    {
                        //ShellOperation.CreateShellLink(srcuri, "title", destdir);
                        throw new InvalidOperationException("Unsupported URL drop");
                    }
                }
                else
                {
                    throw new NotImplementedException($"{operation} operation not supported");
                }
            }
            else
            {
                throw new InvalidOperationException("Invalid drop operation");
            }

            e.Handled = true;
        }

        private async void miRefresh_Activate(object sender, RoutedEventArgs e)
        {
            await App.Current.Database.RefreshAsync();
        }
    }
}

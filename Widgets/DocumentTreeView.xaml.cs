using Datasheets2.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

        public void FocusAndSelectFirst()
        {
            tree.Focus();

            var firstItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
            if (firstItem != null)
                firstItem.IsSelected = true;
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
            if (!(sender as TreeView).IsFocused)
                return;

            // Handle enter on tree view item
            if (e.Key == Key.Enter)
            {
                var item = (sender as TreeView)?.SelectedItem as IItem;
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

        private void tree_MouseMove(object sender, MouseEventArgs e)
        {
            // Simulate behaviour of old Datasheets app where holding mouse button 
            // in the list will keep selecting the item under the mouse as it moves.
            // TODO: Doesn't work if mouse leaves the treeview area
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(tree);

                // Find TreeViewItem under the cursor
                var hit = tree.InputHitTest(pos) as DependencyObject;
                while (!(hit is TreeViewItem) && hit != null)
                {
                    hit = VisualTreeHelper.GetParent(hit);
                }

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
            Process.Start("explorer", App.Current.DocumentsDir);
        }

        private void TreeViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Force selection of item on right click, before context menu is shown
            var treeViewItem = (sender as TreeViewItem);
            treeViewItem.IsSelected = true;
        }

        private void TreeViewItem_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void ContextMenu_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }
    }
}

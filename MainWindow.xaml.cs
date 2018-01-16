using Datasheets2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace Datasheets2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        Database db;
        //private DispatcherTimer treeScrollTimer;

        public Database Database { get { return db; } }

        private string _filterText;
        public string FilterText
        {
            get { return _filterText; }
            set { _filterText = value; OnPropertyChanged("FilterText"); }
        }

        public MainWindow()
        {
            InitializeComponent();

            db = new Database();
            this.DataContext = this;

            //treeScrollTimer = new DispatcherTimer();
            //treeScrollTimer.Interval = TimeSpan.FromMilliseconds(500);
            //treeScrollTimer.Tick += scrollTimer_Callback;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                await db.SaveAsync();
                //this.database.Save();
            }
            catch (Exception ex)
            {
                // If we can't save the db, don't close
                e.Cancel = true;
                App.ErrorHandler(ex);
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //this.tree.DataContext = db;
            //this.DataContext = this;
            //OnPropertyChanged("Database");
            await db.LoadAsync(@"C:\Users\Jared\Documents\PDFs\Datasheets");
            //OnPropertyChanged("Database");
            
            //this.database.Load();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            {
                Filter();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Filter();
        }

        private void Filter()
        {

        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }

        private void tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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

        private void tree_KeyUp(object sender, KeyEventArgs e)
        {
            // Handle enter on tree view item
            if (e.Key == Key.Enter)
            {
                var item = (sender as TreeView)?.SelectedItem as IItem;
                if (item != null)
                    item.OpenItem();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // If pressing down, shift focus to the first element in the tree view
            if (e.Key == Key.Down)
            {
                tree.Focus();

                var firstItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
                firstItem.IsSelected = true;
                e.Handled = true;
            }
        }

        private void tree_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // If pressing up and selected item is 0, shift focus to the search box
            if (e.Key == Key.Up)
            {
                var currentItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(tree.SelectedItem);
                var firstItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
                if (currentItem == firstItem)
                {
                    txtSearchBox.Focus();
                    e.Handled = true;
                }
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
    }
}

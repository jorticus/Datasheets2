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
        public Database Database { get { return db; } }

        enum State { TreeView, Search };
        State state = State.TreeView;

        public MainWindow()
        {
            InitializeComponent();

            db = new Database();
            this.DataContext = this;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                await db.SaveAsync();
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
            // Not sure why I need this. Bindings don't update without setting it here...
            // (Doesn't work if I set in the constructor)
            this.tree.DataContext = this;

            // TODO: Load from settings
            var dir = System.IO.Directory.GetCurrentDirectory();
            await db.LoadAsync(dir);

            txtSearchBox.Focus();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StartSearch();
            }
            else
            {
                // Update filter on every keypress
                Filter();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // If pressing down, shift focus to the first element in the tree view
            if (e.Key == Key.Down)
            {
                tree.FocusAndSelectFirst();
                e.Handled = true;
            }
        }
        
        private void DocumentTreeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // If pressing up and selected item is 0, shift focus to the search box
            if (e.Key == Key.Up)
            {
                if (tree.IsFirstItemSelected)
                {
                    txtSearchBox.Focus();
                    e.Handled = true;
                }
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

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (state == State.TreeView)
            {
                StartSearch();
            }
            else
            {
                CancelSearch();
            }
        }

        private void StartSearch()
        {
            btnSearch.Content = "Cancel";
            txtSearchBox.IsEnabled = false;
            state = State.Search;
            search.Visibility = Visibility.Visible;
            tree.Visibility = Visibility.Collapsed;
            search.BeginSearch(txtSearchBox.Text);
        }

        private void CancelSearch()
        {
            btnSearch.Content = "Search";
            txtSearchBox.IsEnabled = true;
            state = State.TreeView;
            search.CancelSearch();
            tree.Visibility = Visibility.Visible;
            search.Visibility = Visibility.Collapsed;
        }
    }
}

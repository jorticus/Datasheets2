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
        //private string[] SUPPORTED_FILE_EXTS = { ".pdf", ".doc", ".docx" };
        private string[] SUPPORTED_FILE_EXTS = null; // Support all filetypes

        public Database Database { get { return App.Current.Database; } }

        enum State { TreeView, Search };
        State state = State.TreeView;

        public MainWindow()
        {
            InitializeComponent();
 
            this.DataContext = this;

            SetupKeyCommands();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            search.Closed += Search_Closed;
        }

        private void SetupKeyCommands()
        {
            // Set up ESC key command
            // (Won't activate if a ContextMenu was closed via ESC)
            var escCommand = new RoutedUICommand(
                "EscBtnCommand", "EscBtnCommand", typeof(MainWindow),
                new InputGestureCollection { new KeyGesture(Key.Escape, ModifierKeys.None, "Close") });
            CommandBindings.Add(new CommandBinding(escCommand, (sender, e) =>
            {
                this.Close();
            }));

            // Set up Refresh key command (F5)
            var refreshCommand = new RoutedUICommand(
                "RefreshBtnCommand", "RefreshBtnCommand", typeof(MainWindow),
                new InputGestureCollection { new KeyGesture(Key.F5, ModifierKeys.None, "Refresh") });
            CommandBindings.Add(new CommandBinding(refreshCommand, async (sender, e) =>
            {
                // NOTE: Async void function
                await this.Database.RefreshAsync();
            }));
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                await Database.SaveAsync();
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

            Database.PropertyChanged += Database_PropertyChanged;

            await Database.LoadAsync(App.Current.DocumentsDir);

            // Hide search button if not allowed
            btnSearch.Visibility = (App.Current.AllowOnlineSearch) ?
                Visibility.Visible : Visibility.Collapsed;

            txtSearchBox.Focus();
        }

        private void Database_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Database.Filter))
            {
                OnDatabaseFilterChanged(Database.Filter);
            }
        }

        private void OnDatabaseFilterChanged(string filter)
        {
            if (!String.IsNullOrEmpty(filter))
            {
                // TODO: Might be best to de-select here.
                //tree.SelectFirst();
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrWhiteSpace(txtSearchBox.Text))
                {
                    // Search online if no items matched the filter
                    if (Database.Root.VisibleItems == 0)
                    {
                        StartSearch();
                    }
                    else
                    {
                        // Else open the first matching item
                        //if (tree.SelectedItem == null)
                        {
                            tree.SelectFirstDocument();
                        }

                        tree.Focus();
                        tree.SelectedItem?.OpenItem();
                    }
                }
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
                tree.Focus();
                tree.SelectFirst();
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
                    tree.UnfocusTree();
                    e.Handled = true;
                }
            }
        }

        // Called when search is closed after downloading an item to the library
        // (Pressing the cancel button does not invoke this)
        private async void Search_Closed(object sender, EventArgs e)
        {
            if (state == State.Search)
            {
                // Clear filter
                txtSearchBox.Text = "";
                Filter();

                CancelSearch();

                // Shift focus to the search field
                txtSearchBox.Focus();

                // Refresh contents
                await Task.Delay(100); // Workaround - might take a few ticks for the file to appear
                await Database.RefreshAsync();
            }
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
            if (!App.Current.AllowOnlineSearch)
                return;

            try
            {
                btnSearch.Content = "Cancel";
                txtSearchBox.IsEnabled = false;
                state = State.Search;
                search.Visibility = Visibility.Visible;
                tree.Visibility = Visibility.Collapsed;
                search.BeginSearch(txtSearchBox.Text);
            }
            catch (Exception ex)
            {
                App.ErrorHandler(ex.ToString(), "Error searching", fatal: false);
            }
        }

        private void CancelSearch()
        {
            if (!App.Current.AllowOnlineSearch)
                return;

            try
            {
                btnSearch.Content = "Search";
                txtSearchBox.IsEnabled = true;
                state = State.TreeView;
                search.CancelSearch();

                Filter();

                tree.Visibility = Visibility.Visible;
                search.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                App.ErrorHandler(ex.ToString(), "Error cancelling search", fatal: false);
            }
        }
    }
}

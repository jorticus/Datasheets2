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

            // Set up ESC key command
            // (Won't activate if a ContextMenu was closed via ESC)
            var command = new RoutedUICommand(
                "EscBtnCommand", "EscBtnCommand", typeof(MainWindow),
                new InputGestureCollection { new KeyGesture(Key.Escape, ModifierKeys.None, "Close") });
            CommandBindings.Add(new CommandBinding(command, (sender, e) =>
            {
                this.Close();
            }));

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
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

            await Database.LoadAsync(App.Current.DocumentsDir);

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
                    tree.UnfocusTree();
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

        private async void tree_Drop(object sender, DragEventArgs e)
        {
            // Otherwise this doesn't get called if you drop
            tree_DragLeave(sender, e);

            // Determine the requested operation
            var result = tree_DragOperation(sender, e);
            var operation = result.Item1;
            var srcfiles = result.Item2;

            if (srcfiles.Length == 0)
                return;

            var destdir = App.Current.DocumentsDir;

            switch (operation)
            {
                case DragDropEffects.Copy:
                    await ShellOperation.SHFileOperationAsync(ShellOperation.FileOperation.Copy, srcfiles.ToArray(), destdir);
                    break;

                case DragDropEffects.Move:
                    await ShellOperation.SHFileOperationAsync(ShellOperation.FileOperation.Move, srcfiles.ToArray(), destdir);
                    break;

                default:
                    // Not supported
                    break;
            }

            e.Handled = true;
        }

        private Tuple<DragDropEffects, string[]> tree_DragOperation(object sender, DragEventArgs e)
        {
            // Default: Deny drop
            DragDropEffects operation = DragDropEffects.None;
            bool supported = true;

            var filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (filenames.Length != 0)
            {
                if (SUPPORTED_FILE_EXTS != null)
                {
                    // Validate that all files match valid extensions
                    var filetypes = filenames.Select(fname => System.IO.Path.GetExtension(fname).ToLowerInvariant());
                    supported = filetypes.All(ext => SUPPORTED_FILE_EXTS.Contains(ext));
                }

                string desc = (filenames.Length == 1) ?
                    $"{System.IO.Path.GetFileName(filenames[0])}" :
                    //"1 file" :
                    $"{filenames.Length} files";

                // We support this operation, figure out if it's a copy or move operation
                // (linking not supported)
                if (supported)
                {
                    bool moveAllowed = ((e.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move);
                    bool copyAllowed = ((e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy);

                    bool ctrlPressed = (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;

                    if (copyAllowed && ctrlPressed)
                    {
                        operation = DragDropEffects.Copy;
                        dropText.Text = $"Copy {desc} to {App.Current.DocumentsDir}";
                    }
                    else if (moveAllowed)
                    {
                        operation = DragDropEffects.Move;
                        dropText.Text = $"Move {desc} to {App.Current.DocumentsDir}";
                    }
                }
            }

            return new Tuple<DragDropEffects, string[]>(operation, filenames);
        }
        private void tree_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                var result = tree_DragOperation(sender, e);
                e.Effects = result.Item1;
            }
            finally
            {
                //dropOverlay.Visibility = (e.Effects != DragDropEffects.None) ?
                //    Visibility.Visible :
                //    Visibility.Collapsed;
                bool showOverlay = (e.Effects != DragDropEffects.None);
                if (showOverlay != dropOverlay.IsEnabled)
                    dropOverlay.IsEnabled = showOverlay;

                e.Handled = true;
            }
        }

        private void tree_DragLeave(object sender, DragEventArgs e)
        {
            //dropOverlay.Visibility = Visibility.Collapsed;

            if (dropOverlay.IsEnabled)
                dropOverlay.IsEnabled = false;

            e.Handled = true;
        }
    }
}

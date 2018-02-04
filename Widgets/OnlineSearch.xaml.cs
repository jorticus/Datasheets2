using Datasheets2.Models;
using Datasheets2.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

namespace Datasheets2.Widgets
{
    /// <summary>
    /// Interaction logic for OnlineSearch.xaml
    /// </summary>
    public partial class OnlineSearch : UserControl, INotifyPropertyChanged
    {
        private Task searchTask;
        private CancellationTokenSource cts;
        private List<string> temporaryFiles;
        private Dictionary<ISearchResult, string> temporaryFileMap;

        public OnlineSearch()
        {
            InitializeComponent();

            this.DataContext = this;

            temporaryFiles = new List<string>();
            temporaryFileMap = new Dictionary<ISearchResult, string>();

            Items = new ObservableCollection<ISearchResult>();

            Loaded += OnlineSearch_Loaded;
            App.Current.Exit += Current_Exit;
        }

        public ICommand PreviewItemCommand { get { return new RelayCommand((o) => {
            return;
        }); } }

        #region Properties

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(IList<ISearchResult>), typeof(OnlineSearch),
                new PropertyMetadata(null));

        public IList<ISearchResult> Items
        {
            get { return (IList<ISearchResult>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public event EventHandler Closed;

        protected void OnClosed()
        {
            Closed?.Invoke(this, new EventArgs());
        }

        #endregion

        private void FinishSearch()
        {
            CancelSearch();
            OnClosed();
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            // Attempt to clean up. 
            // May not succeed if PDFs are still open
            foreach (var tempfile in temporaryFiles)
            {
                try
                {
                    System.IO.File.Delete(tempfile);
                }
                catch
                {
                    Debug.WriteLine($"Could not delete temporary file '{tempfile}'");
                }
            }
        }

        private async Task SearchAsync(string query, CancellationToken ct)
        {
            try
            {
                // TODO: Try trust-worthy providers first, then fall back to lesser trust-worthy sites.
                var searchProviders = new List<Type>
                {
                    typeof(Search.OctopartAPI),
                    typeof(Search.DatasheetCatalog),
                    typeof(Search.AllDatasheet),
                };

                var tasks = new List<Task>();
                foreach (Type providerType in searchProviders)
                {
                    ISearchProvider provider = (ISearchProvider)Activator.CreateInstance(providerType);
                    provider.ItemFound += Provider_ItemFound;

                    var task = provider.SearchAsync(query, ct);
                    tasks.Add(task);
                }

                searchTask = Task.WhenAll(tasks);

                await searchTask;
            }
            catch (TaskCanceledException)
            {
                // This is expected
            }
            catch (WebException ex)
            {
                IsSearching = false;
                App.ErrorHandler(ex.ToString(), "HTTP Error", fatal:false);
            }
            catch (Exception ex)
            {
                IsSearching = false;
                App.ErrorHandler(ex, fatal:false);

            }
            IsSearching = false;
        }

        private void Provider_ItemFound(object sender, ItemFoundEventArgs e)
        {
            Items.Add(e.Item);
        }

        public void BeginSearch(string query)
        {
            if (IsSearching)
                throw new InvalidOperationException("Search in progress");
            IsSearching = true;
            try
            {
                cts = new CancellationTokenSource();

                Items.Clear();

                // Begin Search Task
                searchTask = SearchAsync(query, cts.Token);
            }
            catch
            {
                // If we failed to start, cancel the progress bar
                IsSearching = false;
                throw;
            }
        }

        public async void CancelSearch()
        {
            if (IsSearching)
            {
                // Request cancellation
                cts.Cancel();

                // Wait for searches to cancel
                await Task.WhenAll(searchTask);

                // Clean up
                searchTask.Dispose();
                searchTask = null;
                cts.Dispose();
                cts = null;
            }

            //IsSearching = false;
        }

        private void OnlineSearch_Loaded(object sender, RoutedEventArgs e)
        {

        }

        bool _isSearching = false;
        protected bool IsSearching
        {
            get { return _isSearching; }
            set
            {
                _isSearching = value;
                progressBar.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private async void list_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var item = ((ListView)sender).SelectedItem as ISearchResult;
                if (item != null)
                {
                    await PreviewDatasheet(item);
                }
            }
        }

        private async void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((ListViewItem)sender).DataContext as ISearchResult;
            if (item != null)
            {
                await PreviewDatasheet(item);
            }
        }

        private async Task<string> DownloadDatasheet(ISearchResult item)
        {
            // Create a new 0 byte temporary file placeholder
            //var tempfile = System.IO.Path.GetTempFileName();
            string temppath = System.IO.Path.GetTempPath();
            var tempfile = System.IO.Path.Combine(temppath, item.Filename);

            // Sanity check - ensure file doesn't end up outside the TEMP folder
            ShellOperation.ValidatePathRoot(tempfile, temppath);

            // Remove existing items
            // TODO: What if we can't remove?
            if (System.IO.File.Exists(tempfile))
                System.IO.File.Delete(tempfile);

            // Download to temporary file
            // TODO: Show progress
            await item.DownloadDatasheetAsync(tempfile);

            // Keep track of it so we can delete it later
            temporaryFiles.Add(tempfile);
            temporaryFileMap[item] = tempfile;

            return tempfile;
        }

        private async Task PreviewDatasheet(ISearchResult item)
        {
            var pdffile = await DownloadDatasheet(item);

            // Shell open the file
            // TODO: This may be potentially dangerous as the file comes from the internet.
            // Windows SHOULD honour the .pdf extension and open in Adobe Reader or equivalent.
            string ext = System.IO.Path.GetExtension(pdffile).ToLowerInvariant();
            if (ext == ".pdf")
            {
                ShellOperation.ShellExecute(pdffile);
            }
            else
            {
                App.ErrorHandler($"Unsupported file type {ext}", fatal: false);
            }
        }

        private async Task DownloadDatasheetToLibrary(ISearchResult item)
        {
            string destfile = System.IO.Path.Combine(App.Current.DocumentsDir, item.Filename);

            string tmpfile;
            if (!temporaryFileMap.TryGetValue(item, out tmpfile))
            {
                // Download file to TEMP if not already downloaded
                tmpfile = await DownloadDatasheet(item);
            }

            if (!System.IO.File.Exists(tmpfile))
                throw new InvalidOperationException($"Temp file has disappeared?? {tmpfile}");

            // Copy the temporary file into the library
            await ShellOperation.SHFileOperationAsync(
                ShellOperation.FileOperation.Move, tmpfile, destfile);

            // FSWatcher should automatically pick up the new item when it's copied into the library directory
            FinishSearch();
        }

        private ISearchResult GetSelectedItem()
        {
            return list.SelectedItem as ISearchResult;
        }

        private async void miPreviewItem_Click(object sender, RoutedEventArgs e)
        {
            // Download & preview the item
            var item = GetSelectedItem();
            if (item != null)
            {
                await PreviewDatasheet(item);
            }
        }

        private async void miDownloadLibrary_Click(object sender, RoutedEventArgs e)
        {
            // Download the item to the library
            var item = GetSelectedItem();
            if (item != null)
            {
                await DownloadDatasheetToLibrary(item);
            }
        }

        private void miOpenWebpage_Click(object sender, RoutedEventArgs e)
        {
            // Open the original webpage
            var item = GetSelectedItem();
            if (item != null)
            {
                ShellOperation.ShellOpenUri(item.WebpageUrl);
            }
        }
    }
}

﻿using Datasheets2.Models;
using Datasheets2.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public OnlineSearch()
        {
            InitializeComponent();

            this.DataContext = this;

            Items = new ObservableCollection<WebSearchItem>();

            Loaded += OnlineSearch_Loaded;
        }

        private async Task SearchAsync(string query, CancellationToken ct)
        {
            var searchProviders = new List<Type>
            {
                typeof(Search.DatasheetCatalog),
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
            try
            {
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

        private IList<WebSearchItem> _items;
        protected IList<WebSearchItem> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                OnPropertyChanged("Items");
                list.ItemsSource = value; // TODO: Why is this required? Why do bindings not just work??
            }
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

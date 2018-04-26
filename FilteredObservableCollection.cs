using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datasheets2
{
    public class FilteredObservableCollection<T> : ObservableCollection<T>
    {
        //private bool isUpdating = false;
        private Func<T, bool> filterPredicate;
        //private string _filter;
        //private IList<T> _filteredItems;
        //private IList<T> _mirror;

        public FilteredObservableCollection()
            : base()
        {
            //_filteredItems = new List<T>();
            //_mirror = new List<T>();
        }

        public FilteredObservableCollection(IEnumerable<T> items)
            : base(items)
        {
            //_filteredItems = new List<T>(items);
            //_mirror = new List<T>(items);
        }

        //public string Filter
        //{
        //    get { return _filter; }
        //    set { _filter = value; FilterUpdated(); }
        //}

        //public int VisibleItems
        //{
        //    get; private set;
        //}

        //private void FilterUpdated()
        //{
        //    foreach (var item in this)
        //    {
        //        bool itemVisible = item.
        //    }
        //}

        //private void UpdateFilteredItems()
        //{
        //    // Re-calculate filter
        //    //this._filteredItems = (filterPredicate != null) ?
        //    //    this.Items.Where(this.filterPredicate).ToList() :
        //    //    this.Items;

        //    //this.Items.Clear();
        //    //= (filterPredicate != null) ?
        //    //    this.Items.Where(this.filterPredicate).ToList() :
        //    //    this.Items;
        //}

        //protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        //{
        //    base.OnCollectionChanged(e);

        //    if (!isUpdating)
        //    {
        //        UpdateFilteredItems();
        //    }

        //}

        //public new T this[int index]
        //{
        //    get { return _filteredItems[index]; }
        //    //set { base[index] = value; }
        //    set { throw new NotSupportedException("Not supported while filtering"); }
        //}

        //public new int Count { get { return _filteredItems.Count; } }

        //public new IEnumerator<T> GetEnumerator()
        //{
        //    return base.GetEnumerator();
        //}

        /*
        public int Filter(Func<T, bool> predicate)
        {
            this.filterPredicate = predicate;
            UpdateFilteredItems();

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            isUpdating = true;
            try
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            finally
            {
                isUpdating = false;
            }

            

            //int nVisible = 0;
            ////List<T> removedItems = new List<T>();
            
            ////this._filteredItems = this.Where(item => predicate(item)).ToList();
            //var filteredItems = this.Items.Where(predicate);
            //nVisible = filteredItems.Count();

            //var removedItems = this.Items.Except(this._filteredItems);
            //var addedItems = this.Items.Intersect(this._filteredItems);

            ////if (addedItems.Count() > 0)
            ////{
            ////    foreach (var item in addedItems)
            ////        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            ////    //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItems.ToList()));
            ////}

            ////if (removedItems.Count() > 0)
            ////{
            ////    foreach (var item in removedItems)
            ////        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            ////    //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems.ToList()));
            ////}

            ////new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            

            ////foreach (var item in this)
            ////{
            ////    if (predicate(item))
            ////    {
            ////        nVisible++;

            ////    }
            ////    else
            ////    {
            ////        removedItems.Add(item);
            ////    }
            ////}

            //return nVisible;

            return 0;
        }
        */

        public void ResetFilter()
        {
            this.Filter(null);
        }

        public FilteredObservableCollection<T> Filter(Func<T, bool> predicate)
        {
            if (predicate == null)
                return this;

            var filteredItems = this.Items.Where(predicate);

            // Create a new observable collection with the filtered items, since 
            // this is easier to do than getting the whole observable collection to work,
            // and it's still okay in terms of speed.
            // TODO: This new filtered collection won't observe any events.
            return new FilteredObservableCollection<T>(filteredItems);
        }

    }
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FontManager.Collections
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        public override event NotifyCollectionChangedEventHandler? CollectionChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                CollectionChanged?.Invoke(this, e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null) return;

            _suppressNotification = true;

            foreach (T item in list)
            {
                Items.Add(item);
            }

            _suppressNotification = false;
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
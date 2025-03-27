using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace DLSS_Swapper.Datatypes
{
    class RefreshableObservableCollection<T> : ObservableCollection<T>
    {
        public RefreshableObservableCollection() : base() { }

        public void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(e);
        }
    }
}

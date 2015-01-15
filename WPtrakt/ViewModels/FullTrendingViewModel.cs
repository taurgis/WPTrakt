using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using WPtrakt.ViewModels;


namespace WPtrakt
{
    public class FullTrendingViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TrendingListItemViewModel> TrendingItems { get; private set; }

        public FullTrendingViewModel()
        {
            TrendingItems = new ObservableCollection<TrendingListItemViewModel>();
        }

        public void ClearTrendingItems()
        {
            this.TrendingItems = new ObservableCollection<TrendingListItemViewModel>();
        }

      
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            try
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (null != handler)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            catch (Exception) { }
        }
    }
}
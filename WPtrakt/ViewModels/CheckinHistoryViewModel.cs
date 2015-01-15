using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WPtrakt.ViewModels;


namespace WPtrakt
{
    public class CheckinHistoryViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ActivityListItemViewModel> HistoryItems { get; private set; }

        public CheckinHistoryViewModel()
        {
            HistoryItems = new ObservableCollection<ActivityListItemViewModel>();
        }

        public void ClearHistoryItems()
        {
            this.HistoryItems = new ObservableCollection<ActivityListItemViewModel>();
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
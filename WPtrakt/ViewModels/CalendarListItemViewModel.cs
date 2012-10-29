using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WPtrakt
{
    public class CalendarListItemViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> Items { get;  set; }
        private string _date;
        public string Date
        {
            get
            {
                return _date;
            }
            set
            {
                if (value != _date)
                {
                    _date = value;
                    NotifyPropertyChanged("Date");
                }
            }
        }
       
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
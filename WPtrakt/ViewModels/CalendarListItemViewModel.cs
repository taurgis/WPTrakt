using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WPtrakt
{
    public class CalendarListItemViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> Items { get;  set; }

        private String _dateString;
        public String DateString
        {
            get
            {
                String[] parts = _dateString.Split('-');
                if (DateTime.Now.Year.Equals(Int32.Parse(parts[0])) && DateTime.Now.Month.Equals(Int32.Parse(parts[1])) && DateTime.Now.Day.Equals(Int32.Parse(parts[2])))
                    return "Today";
                if (DateTime.Now.AddDays(1).Year.Equals(Int32.Parse(parts[0])) && DateTime.Now.AddDays(1).Month.Equals(Int32.Parse(parts[1])) && DateTime.Now.AddDays(1).Day.Equals(Int32.Parse(parts[2])))
                    return "Tomorrow";

                return _dateString;
            }
            set
            {
                if (value != _dateString)
                {
                    _dateString = value;
                    NotifyPropertyChanged("DateString");
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
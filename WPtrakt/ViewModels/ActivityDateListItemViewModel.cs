using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using WPtrakt.Model;

namespace WPtrakt.ViewModels
{
    public class ActivityDateListItemViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ActivityListItemViewModel> Items { get; set; }
        private DateTime _date;
        public DateTime Date
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


        public String DateString
        {
            get
            {
                if (this.Date.Day == DateTime.Now.Day && this.Date.Month == DateTime.Now.Month && this.Date.Year == DateTime.Now.Year)
                {
                    return "Today";
                }
                else if (this.Date.Day == DateTime.Now.AddDays(-1).Day && this.Date.Month == DateTime.Now.Month && this.Date.Year == DateTime.Now.Year)
                {
                    return "Yesterday";
                }
                else
                    return this.Date.ToShortDateString();

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

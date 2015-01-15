using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;


namespace WPtrakt
{
    public class UpcommingViewModel : INotifyPropertyChanged
    {
        private Boolean LoadingCalendar { get; set; }
        public ObservableCollection<CalendarListItemViewModel> CalendarItems { get;  set; }

        public UpcommingViewModel()
        {
            this.CalendarItems = new ObservableCollection<CalendarListItemViewModel>();
        }
       

        private void RefreshMyShowsView()
        {
            NotifyPropertyChanged("ShowItems");
            NotifyPropertyChanged("LoadingStatus");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
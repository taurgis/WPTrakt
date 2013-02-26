using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.ViewModels;


namespace WPtrakt
{
    public class ActivityViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ActivityListItemViewModel> Activity { get; private set; }

        public ActivityViewModel()
        {
            _loadingStatus = "Collapsed";
            NotifyPropertyChanged("LoadingStatus");
        }

        private String _loadingStatus;
        public String LoadingStatus
        {
            get
            {  
               return _loadingStatus;
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
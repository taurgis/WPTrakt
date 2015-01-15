using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.ViewModels;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;


namespace WPtrakt
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TrendingViewModel> TrendingItems { get; private set; }
        public ListItemViewModel WatchingNow { get; set; }
        public ObservableCollection<ActivityListItemViewModel> HistoryItems { get; private set; }
        public List<TraktActivity> history;
        public ObservableCollection<ListItemViewModel> RecentItems { get; private set; }
        public Boolean LoadingTrendingItems { get; set; }
        public Boolean LoadingHistory { get; set; }

        public MainViewModel()
        {
            this.TrendingItems = new ObservableCollection<TrendingViewModel>();
            this.HistoryItems = new ObservableCollection<ActivityListItemViewModel>();
            this.RecentItems = new ObservableCollection<ListItemViewModel>();
        }

        #region Getters/Setters

        public void clearHistory()
        {
            this.HistoryItems = new ObservableCollection<ActivityListItemViewModel>();
        }

        public void clearRecent()
        {
            this.RecentItems = new ObservableCollection<ListItemViewModel>();
        }

        private TraktProfile _profile;
        public TraktProfile Profile
        {
            get
            {
                return _profile;
            }
            set
            {
                _profile = value;
            }
        }

        public String MainVisibility
        {
            get
            {
                if (Profile != null)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public Boolean PanoramaEnabled
        {
            get
            {
                if (Profile == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public void ClearTrending()
        {
            this.TrendingItems = new ObservableCollection<TrendingViewModel>();

            App.ViewModel.NotifyPropertyChanged("TrendingItems");
        }

  

        #endregion

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

        internal void clearWatching()
        {
            this.WatchingNow = null;
        }
    }
}
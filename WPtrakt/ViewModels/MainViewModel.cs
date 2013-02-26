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
        public ObservableCollection<ListItemViewModel> UpNextItems { get; private set; }
        public ObservableCollection<ListItemViewModel> WatchingNow { get; private set; }
        public ObservableCollection<ActivityListItemViewModel> HistoryItems { get; private set; }
        public List<TraktActivity> history;
        private Main mainPage;
        public Boolean LoadingTrendingItems { get; set; }
        public Boolean LoadingHistory { get; set; }

        public MainViewModel()
        {
            this.TrendingItems = new ObservableCollection<TrendingViewModel>();
            this.HistoryItems = new ObservableCollection<ActivityListItemViewModel>();
            this.UpNextItems = new ObservableCollection<ListItemViewModel>();
            this.WatchingNow = new ObservableCollection<ListItemViewModel>();
        }

        public void SetMainPage(Main mainPage)
        {
            this.mainPage = mainPage;
        }

        #region Getters/Setters

        public void clearHistory()
        {
            this.HistoryItems = new ObservableCollection<ActivityListItemViewModel>();
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

        public Uri SearchIcon
        {
            get
            {
                bool dark = ((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"] == Visibility.Visible);
                if (dark)
                    return new Uri("Images/appbar.search.rest.white.png", UriKind.Relative);
                else
                    return new Uri("Images/appbar.search.rest.black.png", UriKind.Relative);

            }
        }

        private BitmapImage _userAvatar;
        public BitmapImage UserAvatar
        {
            get
            {
                if (_userAvatar == null && (Profile != null))
                {
                    String fileName = "profile.jpg";

                    if (StorageController.doesFileExist(fileName))
                    {
                        _userAvatar = ImageController.getImageFromStorage(fileName);
                    }
                    else
                    {
                        HttpWebRequest request;

                        request = (HttpWebRequest)WebRequest.Create(new Uri(Profile.Avatar));
                        request.BeginGetResponse(new AsyncCallback(request_OpenAvatarCompleted), new object[] { request });

                        return null;
                    }

                    return _userAvatar;
                }
                else
                {
                    return _userAvatar;
                }
            }

            set
            {
                _userAvatar = value;
                NotifyPropertyChanged("UserAvatar");
            }
        }

        void request_OpenAvatarCompleted(IAsyncResult r)
        {
            object[] param = (object[])r.AsyncState;
            HttpWebRequest httpRequest = (HttpWebRequest)param[0];

            HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
            System.Net.HttpStatusCode status = httpResoponse.StatusCode;
            if (status == System.Net.HttpStatusCode.OK)
            {
                Stream str = httpResoponse.GetResponseStream();

                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.UserAvatar = ImageController.saveImage("profile.jpg", str, 100, 90);
                }));
            }

        }

        public String UserName
        {
            get
            {
                if (Profile != null)
                    return Profile.Username;
                else
                    return "";
            }
        }

        public String UserAbout
        {
            get
            {
                if (Profile != null)
                    return (!String.IsNullOrEmpty(Profile.Location) ? Profile.Location : "Omicron Persei 8") + ((!String.IsNullOrEmpty(Profile.About) ? ", " + Profile.About : ""));
                else
                    return "";
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

        #region Profile

        public void RefreshProfile()
        {
            NotifyPropertyChanged("UserAvatar");
            NotifyPropertyChanged("UserName");
            NotifyPropertyChanged("UserAbout");
        }

        #endregion

        #region History

      
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

        internal void clearUpNextItems()
        {
            this.UpNextItems = new ObservableCollection<ListItemViewModel>();
        }

        internal void clearWatching()
        {
            this.WatchingNow = new ObservableCollection<ListItemViewModel>();
        }
    }
}
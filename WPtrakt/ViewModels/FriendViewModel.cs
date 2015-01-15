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
    public class FriendViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ActivityListItemViewModel> HistoryItems { get; private set; }
        public List<TraktActivity> history;
        public Boolean LoadingHistory { get; set; }

        public FriendViewModel()
        {
            this.HistoryItems = new ObservableCollection<ActivityListItemViewModel>();
        }

        #region Getters/Setters

        public void clearHistory()
        {
            this.HistoryItems = new ObservableCollection<ActivityListItemViewModel>();
      
            NotifyPropertyChanged("HistoryItems");
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

 
        public String UserAvatar
        {
            get
            {
                if (Profile != null)
                    return Profile.Avatar;
                else
                    return "";
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
                if (Profile != null && Profile.Stats != null)
                    return (!String.IsNullOrEmpty(Profile.Location) ? Profile.Location : "Omicron Persei 8") + ((!String.IsNullOrEmpty(Profile.About) ? ", " + Profile.About : ""));
                else
                    return "Profile is private";
            }
        }

        public String Collection
        {
            get
            {
                if (Profile != null && Profile.Stats != null)
                    return Profile.Stats.Movies.Collection + " movies, " + Profile.Stats.Shows.Collection + " shows and " + Profile.Stats.Episodes.Collection + " episodes";
                else
                    return "Profile is private";
            }
        }

        public String Watched
        {
            get
            {
                if (Profile != null && Profile.Stats != null)
                    return Profile.Stats.Movies.Watched + " movies, " + Profile.Stats.Shows.Watched + " shows and " + Profile.Stats.Episodes.Watched + " episodes";
                else
                    return "Profile is private";
            }
        }


        public String Opinion
        {
            get
            {
                if (Profile != null && Profile.Stats != null)
                    return Profile.Username + " liked " + Profile.Stats.Movies.Loved + " movies, " + Profile.Stats.Shows.Loved + " shows and " + Profile.Stats.Episodes.Loved + " episodes. But doesn't like " + Profile.Stats.Movies.Hated + " movies, " + Profile.Stats.Shows.Hated + " shows and " + Profile.Stats.Episodes.Hated + " episodes";
                else
                    return "Profile is private";
            }
        }  

        #endregion

        #region Profile

        public void RefreshProfile()
        {
            NotifyPropertyChanged("UserAvatar");
            NotifyPropertyChanged("UserName");
            NotifyPropertyChanged("UserAbout");
            NotifyPropertyChanged("Collection");
            NotifyPropertyChanged("Watched");
            NotifyPropertyChanged("Opinion");
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
    }
}
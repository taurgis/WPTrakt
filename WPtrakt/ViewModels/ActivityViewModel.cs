using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using VPtrakt.Controllers;
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

        public void LoadData()
        {
            _loadingStatus = "Visible";
            NotifyPropertyChanged("LoadingStatus");
            this.Activity = new ObservableCollection<ActivityListItemViewModel>();
            NotifyPropertyChanged("Activity");

            var movieClient = new WebClient();
            movieClient.Encoding = Encoding.GetEncoding("UTF-8");
            movieClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMovieStringCompleted);
            movieClient.UploadStringAsync(new Uri("http://api.trakt.tv/activity/friends.json/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication());
        }


        void client_UploadMovieStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                Activity = new ObservableCollection<ActivityListItemViewModel>();
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktFriendsActivity));
                    Console.Write(jsonString);
                    TraktFriendsActivity friendsActivity = (TraktFriendsActivity)ser.ReadObject(ms);
                    foreach (TraktActivity activity in friendsActivity.Activity)
                    {
                        try
                        {
                            switch (activity.Action)
                            {
                                case "watchlist":
                                    AddToWatchList(activity);
                                    break;
                                case "rating":
                                    Rated(activity);
                                    break;
                                case "checkin":
                                    Checkin(activity);
                                    break;
                                case "scrobble":
                                    Scrobble(activity);
                                    break;
                                case "collection":
                                    Collection(activity);
                                    break;
                                case "shout":
                                    Shout(activity);
                                    break;
                            }
                        }
                        catch (NullReferenceException) { }
                    }
                    _loadingStatus = "Collapsed";
                    NotifyPropertyChanged("Activity");
                    NotifyPropertyChanged("LoadingStatus");
                }
                 
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void Shout(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added shout to movie " + activity.Movie.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Imdb = activity.Movie.imdb_id, Avatar = activity.User.Avatar, Type = "movie" });
                    break;
                case "show":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added shout to show " + activity.Show.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "show" });
                    break;
                case "episode":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added shout to episode " + activity.Episode.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number) });
                    break;
            }
        }

        private void Collection(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added " + activity.Movie.Title + " to the collection.", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Imdb = activity.Movie.imdb_id, Avatar = activity.User.Avatar, Type = "movie" });
                    break;
                case "show":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added " + activity.Show.Title + " to the collection.", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "show" });
                    break;
                case "episode":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added " + activity.Episode.Title + "to the collection", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number) });
                    break;
            }
        }

        private void Scrobble(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " scrobbled " + activity.Movie.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Imdb = activity.Movie.imdb_id, Avatar = activity.User.Avatar, Type = "movie" });
                    break;
                case "show":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " scrobbled " + activity.Show.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "show" });
                    break;
                case "episode":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " scrobbled " + activity.Episode.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number) });
                    break;
            }
        }

        private void Checkin(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " checked in " + activity.Movie.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Imdb = activity.Movie.imdb_id, Avatar = activity.User.Avatar, Type = "movie" });
                    break;
                case "show":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " checked in " + activity.Show.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "show" });
                    break;
                case "episode":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " checked in " + activity.Episode.Title + ".", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number) });
                    break;
            }
        }

        private void Rated(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " rated " + activity.Movie.Title + ": " + activity.RatingAdvanced + "/10 .", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Imdb = activity.Movie.imdb_id, Avatar = activity.User.Avatar, Type = "movie" });
                    break;
                case "show":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " rated " + activity.Show.Title + ": " + activity.RatingAdvanced + "/10 .", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "show" });
                    break;
                case "episode":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " rated " + activity.Episode.Title + ": " + activity.RatingAdvanced + "/10 .", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number) });
                    break;
            }
        }

        private void AddToWatchList(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added " + activity.Movie.Title + " to the watchlist.", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Imdb = activity.Movie.imdb_id, Avatar = activity.User.Avatar, Type = "movie" });
                    break;
                case "show":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added " + activity.Show.Title + " to the watchlist.", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "show" });
                    break;
                case "episode":
                    this.Activity.Add(new ActivityListItemViewModel() { Activity = activity.User.Username + " added " + activity.Episode.Title + "to the watchlist", Name = activity.User.Username, Time = activity.When.Day + " - " + activity.When.Time, Tvdb = activity.Show.tvdb_id, Avatar = activity.User.Avatar, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number) });
                    break;
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
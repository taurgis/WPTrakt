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
using System.Windows.Threading;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.ViewModels;


namespace WPtrakt
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> TrendingItems { get; private set; }
        public ObservableCollection<ActivityDateListItemViewModel> HistoryItems { get; private set; }
        private List<TraktActivity> history;
        private DateTime firstCall { get; set; }
        public Boolean LoadingTrendingItems { get; set; }
        public Boolean LoadingHistory { get; set;} 
        public MainViewModel()
        {
            this.TrendingItems = new ObservableCollection<ListItemViewModel>();
            this.HistoryItems = new ObservableCollection<ActivityDateListItemViewModel>();
        }

        #region Getters/Setters

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

        public String LoadingStatus
        {
            get
            {
                if (Profile == null || this.LoadingHistory || this.LoadingTrendingItems)
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

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        #endregion

        #region Profile

        DispatcherTimer userValidationTimer;

        public void LoadData()
        {
            this.Profile = null;
            this.IsDataLoaded = true;

            CallValidationService();
        }

        private void CallValidationService()
        {
            firstCall = DateTime.Now;
            userValidationTimer = new DispatcherTimer();
            userValidationTimer.Interval = TimeSpan.FromSeconds(2);
            userValidationTimer.Tick += OnTimerTick;
            userValidationTimer.Start();

            HttpWebRequest request;

            request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/account/test/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
            request.Method = "POST";
            request.BeginGetRequestStream(new AsyncCallback(GetValidationRequestStreamCallback), request);
        }


        void GetValidationRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadValidationStringCompleted), webRequest);
        }

        void client_DownloadValidationStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                HttpStatusCode status = httpResoponse.StatusCode;

                if (status == System.Net.HttpStatusCode.OK)
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        userValidationTimer.Stop();

                        LoadProfile();
                    });
                }
            }
            catch (WebException)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ToastNotification.ShowToast("User incorrect!", "Login data incorrect, or server connection problems.");
                    userValidationTimer.Stop();

                    Profile = new TraktProfileWithWatching();
                    NotifyPropertyChanged("LoadingStatus");
                });
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

        }

        void OnTimerTick(object sender, EventArgs e)
        {
            int seconds = (DateTime.Now - firstCall).Seconds;

            if (seconds > 10)
            {
                ToastNotification.ShowToast("Connection", "Slow connection to trakt!");
                userValidationTimer.Stop();
            }
        }

        private void LoadProfile()
        {
            if (StorageController.doesFileExist(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json"))
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = false;
                worker.WorkerSupportsCancellation = false;
                worker.DoWork += new DoWorkEventHandler(profileworker_DoWork);

                worker.RunWorkerAsync();
            }
            else
            {
                CallProfileService();
            }

        }

        void profileworker_DoWork(object sender, DoWorkEventArgs e)
        {
            
            Profile = (TraktProfile)StorageController.LoadObject(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json", typeof(TraktProfileWithWatching));
   

                //Cache the profile for 4 hours, the history is prone to quick change. Though 4 hours is enough to catch server problems.
            if ((DateTime.Now - Profile.DownloadTime).Hours < 2)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    RefreshProfile();
                });
            }
            else
            {
                CallProfileService();
            }
        }

        private void CallProfileService()
        {
            HttpWebRequest request;

            request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/user/profile.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
            request.Method = "POST";
            request.BeginGetRequestStream(new AsyncCallback(GetProfileRequestStreamCallback), request);
        }


        void GetProfileRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadProfileStringCompleted), webRequest);
        }

        void client_DownloadProfileStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                System.Net.HttpStatusCode status = httpResoponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    String jsonString = new StreamReader(httpResoponse.GetResponseStream()).ReadToEnd();

                    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                    {
                        if (jsonString.Contains("watching\":[]"))
                        {
                            var ser = new DataContractJsonSerializer(typeof(TraktProfile));

                            this.Profile = (TraktProfile)ser.ReadObject(ms);

                            StorageController.saveObject(this.Profile, typeof(TraktProfile));
                        }
                        else
                        {
                            var ser = new DataContractJsonSerializer(typeof(TraktProfileWithWatching));

                            this.Profile = (TraktProfileWithWatching)ser.ReadObject(ms);

                            StorageController.saveObject(this.Profile, typeof(TraktProfileWithWatching));
                        }
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            NotifyPropertyChanged("HistoryItems");
                            RefreshProfile();
                        });
                    }
                }

            }
            catch (WebException)
            {
                Profile = new TraktProfileWithWatching();
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NotifyPropertyChanged("MainVisibility");
                    NotifyPropertyChanged("LoadingStatus");
                    ErrorManager.ShowConnectionErrorPopup();
                });
            }
            catch (TargetInvocationException)
            {
                NotifyPropertyChanged("MainVisibility");
                NotifyPropertyChanged("LoadingStatus");
                ErrorManager.ShowConnectionErrorPopup();
            }

        }

        private void RefreshProfile()
        {
            NotifyPropertyChanged("UserAvatar");
            NotifyPropertyChanged("UserName");
            NotifyPropertyChanged("UserAbout");
            NotifyPropertyChanged("LoadingStatus");
            NotifyPropertyChanged("MainVisibility");
            NotifyPropertyChanged("PanoramaEnabled");
        }

        #endregion

        #region Trending

        public void loadTrending()
        {
            if (!LoadingTrendingItems)
            {
                this.LoadingTrendingItems = true;
                this.TrendingItems = new ObservableCollection<ListItemViewModel>();

                NotifyPropertyChanged("TrendingItems");
                NotifyPropertyChanged("LoadingStatus");

                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/movies/trending.json/9294cac7c27a4b97d3819690800aa2fedf0959fa"));
                request.Method = "POST";
                request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);
            }
        }

        void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadTrendingStringCompleted), webRequest);
        }

        void client_DownloadTrendingStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                System.Net.HttpStatusCode status = httpResoponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    String jsonString = new StreamReader(httpResoponse.GetResponseStream()).ReadToEnd();

                    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                    {
                        //parse into jsonser
                        var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                        TraktMovie[] obj = (TraktMovie[])ser.ReadObject(ms);
                        this.TrendingItems = new ObservableCollection<ListItemViewModel>();
                        int count = 0;
                        foreach (TraktMovie traktMovie in obj)
                        {
                            if (++count > 8)
                                break;

                            TraktMovie movie = traktMovie;

                            this.TrendingItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, Watched = movie.Watched, Rating = movie.MyRatingAdvanced, NumberOfRatings = movie.Ratings.Votes.ToString(), Type = "Movie", InWatchList = movie.InWatchlist, SubItemText = movie.year.ToString() });
                        }

                        this.LoadingTrendingItems = false;

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            NotifyPropertyChanged("TrendingItems");
                            NotifyPropertyChanged("LoadingStatus");
                        });
                    }
                }
            }
            catch (WebException)
            { }
        }
        #endregion

        #region History

        public void LoadHistoryData()
        {
            this.LoadingHistory = true;
            NotifyPropertyChanged("LoadingStatus");
           
            var historyClient = new WebClient();
            historyClient.Encoding = Encoding.GetEncoding("UTF-8");
            historyClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMovieStringCompleted);
            historyClient.UploadStringAsync(new Uri("http://api.trakt.tv/activity/user.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
        }


        void client_UploadMovieStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                this.HistoryItems = new ObservableCollection<ActivityDateListItemViewModel>();
                this.history = new List<TraktActivity>();

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktFriendsActivity));
          
                    TraktFriendsActivity friendsActivity = (TraktFriendsActivity)ser.ReadObject(ms);
                    int counter = 0;
                    DateTime lastDateTime = DateTime.Now;
                    foreach (TraktActivity activity in friendsActivity.Activity)
                    {
                        history.Add(activity);
                        ActivityListItemViewModel tempModel = null;
                        try
                        {
                            if (counter++ <= 20)
                            {
                                switch (activity.Action)
                                {
                                    case "watchlist":
                                        tempModel = AddToWatchList(activity);
                                        break;
                                    case "rating":
                                        tempModel = Rated(activity);
                                        break;
                                    case "checkin":
                                        tempModel = Checkin(activity);
                                        break;
                                    case "scrobble":
                                        tempModel = Scrobble(activity);
                                        break;
                                    case "collection":
                                        tempModel = Collection(activity);
                                        break;
                                    case "shout":
                                        tempModel = Shout(activity);
                                        break;
                                }


                                OrderHistory(activity, tempModel);
                            }
                        }
                        catch (NullReferenceException) { }
                    }

                   
                    ms.Close();
                    this.LoadingHistory = false;
                    NotifyPropertyChanged("HistoryItems");
                    NotifyPropertyChanged("LoadingStatus");
                }

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void OrderHistory(TraktActivity activity, ActivityListItemViewModel tempModel)
        {
            if (HistoryItems.Count == 00)
            {
                DateTime time = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                time = time.AddSeconds(activity.TimeStamp);
                time = time.ToLocalTime();
                DateTime onlyDay = new DateTime(time.Year, time.Month, time.Day);

                ActivityDateListItemViewModel firstActivity = new ActivityDateListItemViewModel();
                firstActivity.Items = new ObservableCollection<ActivityListItemViewModel>();
                firstActivity.Items.Add(tempModel);
                firstActivity.Date = onlyDay;
                HistoryItems.Add(firstActivity);
            }
            else
            {
                Boolean added = false;
                DateTime time = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                time = time.AddSeconds(activity.TimeStamp);
                time = time.ToLocalTime();
                foreach (ActivityDateListItemViewModel model in HistoryItems)
                {

                    if (model.Date.Year == time.Year && model.Date.Month == time.Month && model.Date.Day == time.Day)
                    {
                        model.Items.Add(tempModel);
                        added = true;
                        break;
                    }

                }
                if (!added)
                {

                    DateTime onlyDay = new DateTime(time.Year, time.Month, time.Day);
                    ActivityDateListItemViewModel newDateActivity = new ActivityDateListItemViewModel();
                    newDateActivity.Items = new ObservableCollection<ActivityListItemViewModel>();
                    newDateActivity.Items.Add(tempModel);
                    newDateActivity.Date = onlyDay;
                    HistoryItems.Add(newDateActivity);

                }

            }
        }

        public void FilterHistory(int type)
        {
            int counter = 0;
            this.HistoryItems = new ObservableCollection<ActivityDateListItemViewModel>();
            foreach (TraktActivity activity in history)
            {
                ActivityListItemViewModel tempModel = null;

                try
                {
                    if (counter++ <= 20)
                    {
                        switch (activity.Action)
                        {
                            case "watchlist":
                                if (type == 1 || type == 0)
                                   tempModel = AddToWatchList(activity);
                                break;
                            case "rating":
                                if (type == 2 || type == 0)
                                    tempModel = Rated(activity);
                                break;
                            case "checkin":
                                if (type == 3 || type == 0)
                                    tempModel = Checkin(activity);
                                break;
                            case "scrobble":
                                if (type == 4 || type == 0)
                                    tempModel = Scrobble(activity);
                                break;
                            case "collection":
                                if (type == 5 || type == 0)
                                    tempModel = Collection(activity);
                                break;
                            case "shout":
                                if (type == 6 || type == 0)
                                    tempModel = Shout(activity);
                                break;
                        }

                        if(tempModel != null)
                            OrderHistory(activity, tempModel);
                    }
                }
                catch (NullReferenceException) { }
            }


            if (HistoryItems.Count == 0)
            {
               // this.HistoryItems.Add(new ActivityListItemViewModel() { Activity = "No recent actions found. " });
            }

            NotifyPropertyChanged("HistoryItems");
        }

        private ActivityListItemViewModel Shout(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = "added shout to movie " + activity.Movie.Title + ".\r\n\r\n" + activity.Movie.year.ToString(), Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = "added shout to show " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                     return new ActivityListItemViewModel() { Activity = "added shout to episode " + activity.Show.Title + " - " + activity.Show.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Collection(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                     return new ActivityListItemViewModel() { Activity = "added " + activity.Movie.Title + " to the collection.\r\n\r\n" + activity.Movie.year.ToString(), Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                     return new ActivityListItemViewModel() { Activity = "added " + activity.Show.Title + " to the collection.", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                     return new ActivityListItemViewModel() { Activity = "added " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + "to the collection", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Scrobble(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                     return new ActivityListItemViewModel() { Activity = "scrobbled " + activity.Movie.Title + ".\r\n\r\n" + activity.Movie.year.ToString(), Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                     return new ActivityListItemViewModel() { Activity = "scrobbled " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                     return new ActivityListItemViewModel() { Activity = "scrobbled " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Checkin(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = "checked in " + activity.Movie.Title + ". \r\n\r\n" + activity.Movie.year.ToString(), Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = "checked in " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = "checked in " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Rated(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = "rated " + activity.Movie.Title + ": " + activity.RatingAdvanced + "/10 .\r\n\r\n" + activity.Movie.year.ToString(), Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                     return new ActivityListItemViewModel() { Activity = "rated " + activity.Show.Title + ": " + activity.RatingAdvanced + "/10 .", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = "rated " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ": " + activity.RatingAdvanced + "/10 .", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }
            return null;
        }

        private ActivityListItemViewModel AddToWatchList(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                     return new ActivityListItemViewModel() { Activity = "added " + activity.Movie.Title + " to the watchlist.\r\n\r\n" + activity.Movie.year.ToString(), Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = "added " + activity.Show.Title + " to the watchlist.", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id,  Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                     return new ActivityListItemViewModel() { Activity = "added " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + "to the watchlist", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        #endregion

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
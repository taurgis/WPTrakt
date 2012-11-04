using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;


namespace WPtrakt
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> TrendingItems { get; private set; }
        public ObservableCollection<ListItemViewModel> HistoryItems { get; private set; }

        private DateTime firstCall { get; set; }
        public Boolean LoadingTrendingItems { get; set; }

        public MainViewModel()
        {
            this.TrendingItems = new ObservableCollection<ListItemViewModel>();

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
                if (Profile == null)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        private String _loadingStatusTrending;
        public String LoadingStatusTrending
        {
            get
            {
                return _loadingStatusTrending;
            }
            set
            {
                _loadingStatusTrending = value;
                NotifyPropertyChanged("LoadingStatusTrending");
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

                    Profile = new TraktProfile();
                    NotifyPropertyChanged("LoadingStatus");
                });
            }
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
            Profile = (TraktProfile)StorageController.LoadObject(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json", typeof(TraktProfile));
            //Cache the profile for 4 hours, the history is prone to quick change. Though 4 hours is enough to catch server problems.
            if ((DateTime.Now - Profile.DownloadTime).Hours < 4)
            {
                loadHistory();

                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NotifyPropertyChanged("HistoryItems");
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
                        var ser = new DataContractJsonSerializer(typeof(TraktProfile));
                       
                        this.Profile = (TraktProfile)ser.ReadObject(ms);

                        StorageController.saveObject(this.Profile, typeof(TraktProfile));
                        loadHistory();

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
                Profile = new TraktProfile();
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NotifyPropertyChanged("MainVisibility");
                    NotifyPropertyChanged("LoadingStatus");
                    ErrorManager.ShowConnectionErrorPopup();
                });
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
            NotifyPropertyChanged("HistoryItems");
        }

        private void loadHistory()
        {
            this.HistoryItems = new ObservableCollection<ListItemViewModel>();

            foreach (TraktWatched watched in Profile.Watched)
            {
                if (watched.Episode != null)
                    this.HistoryItems.Add(new ListItemViewModel() { Name = watched.Episode.Title, ImageSource = watched.Episode.Images.Screen, Imdb = watched.Show.imdb_id + watched.Episode.Season + watched.Episode.Number, SubItemText = "Season " + watched.Episode.Season + ", Episode " + watched.Episode.Number, Episode = watched.Episode.Number, Season = watched.Episode.Season, Tvdb = watched.Show.tvdb_id, Type = "episode", WatchedTime = Int32.Parse(watched.Watched) });
                else if (watched.Movie != null)
                    this.HistoryItems.Add(new ListItemViewModel() { Name = watched.Movie.Title, ImageSource = watched.Movie.Images.Poster, Imdb = watched.Movie.imdb_id, SubItemText = "Runtime: " + watched.Movie.Runtime + " mins\r\n" + watched.Movie.year.ToString(), Type = "movie" });
            }

            if (this.HistoryItems.Count == 0)
                this.HistoryItems.Add(new ListItemViewModel() { Name = "No recent history" });
        }

        #endregion

        #region Trending

        public void loadTrending()
        {
            if (!LoadingTrendingItems)
            {
                this.LoadingTrendingItems = true;
                this.LoadingStatusTrending = "Visible";
                this.TrendingItems = new ObservableCollection<ListItemViewModel>();

                NotifyPropertyChanged("TrendingItems");
                NotifyPropertyChanged("LoadingStatusTrending");

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
                            this.LoadingStatusTrending = "Collapsed";
                        });
                    }
                }
            }
            catch (WebException)
            { }
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
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using System.Windows;

namespace WPtrakt
{
    public class EpisodeViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ShoutItems { get; private set; }
        public Boolean ShoutsLoaded { get; set; }

        public EpisodeViewModel()
        {
            ShoutsLoaded = false;
            ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." }); 
        }

        #region Getters/Setters

        private string _name;
        public string Name
        {
            get
            {
                if(String.IsNullOrEmpty(_name))
                    return "Loading..";
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private string _showName;
        public string ShowName
        {
            get
            {

                return _showName;
            }
            set
            {
                if (value != _showName)
                {
                    _showName = value;
                }
            }
        }

        private Int16 _showYear;
        public Int16 ShowYear
        {
            get
            {

                return _showYear;
            }
            set
            {
                if (value != _showYear)
                {
                    _showYear = value;
                }
            }
        }

        private long _airDate;
        public string AirDate
        {
            get
            {
                DateTime time = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                time = time.AddSeconds(_airDate);
                return time.ToLongDateString();
            }
        }

        private string _imdb;
        public string Imdb
        {
            get
            {
                return _imdb;
            }
            set
            {
                if (value != _imdb)
                {
                    _imdb = value;
                    NotifyPropertyChanged("Imdb");
                }
            }
        }

        private string _season;
        public string Season
        {
            get
            {
                return _season;
            }
            set
            {
                if (value != _season)
                {
                    _season = value;
                    NotifyPropertyChanged("Season");
                }
            }
        }

            private string _tvdb;
        public string Tvdb
        {
            get
            {
                return _tvdb;
            }
            set
            {
                if (value != _tvdb)
                {
                    _tvdb = value;
                    NotifyPropertyChanged("Tvdb");
                }
            }
        }


        private string _number;
        public string Number
        {
            get
            {
                return _number;
            }
            set
            {
                if (value != _number)
                {
                    _number = value;
                    NotifyPropertyChanged("Number");
                }
            }
        }

        private string _overview;
        public string Overview
        {
            get
            {
                return _overview;
            }
            set
            {
                if (value != _overview)
                {
                    _overview = value;
                    NotifyPropertyChanged("Overview");
                }
            }
        }

        private Boolean _watched;
        public Boolean Watched
        {
            get
            {
                return _watched;
            }
            set
            {
                if (value != _watched)
                {
                    _watched = value;
                }
            }
        }

        private Int16 _rating;
        public Int16 Rating
        {
            get
            {
                return _rating;
            }
            set
            {
                if (value != _rating)
                {
                    _rating = value;
                }
            }
        }

        private Int16 _votes;
        public Int16 Votes
        {
            get
            {
                return _votes;
            }
            set
            {
                if (value != _votes)
                {
                    _votes = value;
                }
            }
        }
        public String RatingString
        {
            get
            {
                String baseString;
                baseString = this.Rating / 10 + "/10 (" + this.Votes + ")";

                if (this.MyRating == null)
                    return "N/A";

                if (!(this.MyRatingAdvanced == 0 && this.MyRating.Equals("false")))
                {
                    if (this.MyRatingAdvanced == 0 && !(this.MyRating.Equals("false")))
                    {
                        baseString += " - Mine: " + this.MyRating;
                    }
                    else
                    {
                        baseString += " - Mine: " + this.MyRatingAdvanced + "/10";
                    }
                }
                return baseString;
            }
        }

        private Int16 _myRatingAdvanced;
        public Int16 MyRatingAdvanced
        {
            get
            {
                return _myRatingAdvanced;
            }
            set
            {
                if (value != _myRatingAdvanced)
                {
                    _myRatingAdvanced = value;
                }
            }
        }

        private String _myRating;
        public String MyRating
        {
            get
            {
                return _myRating;
            }
            set
            {
                if (value != _myRating)
                {
                    _myRating = value;
                }
            }
        }


        private string _fanart;
        public string Fanart
        {
            get
            {
                return _fanart;
            }
            set
            {
                if (value != _fanart)
                {
                    _fanart = value;
                    NotifyPropertyChanged("Fanart");
                }
            }
        }

        private string _screen;
        public string Screen
        {
            get
            {
                return _screen;
            }
            set
            {
                if (value != _screen)
                {
                    _screen = value;
                    NotifyPropertyChanged("Screen");
                }
            }
        }

        private BitmapImage _backgroundImage;
        public BitmapImage BackgroundImage
        {
            get
            {
                if (Fanart == null)
                    return null;

                if (_backgroundImage == null)
                {
                    LoadBackgroundImage();

                    return _backgroundImage;
                }
                else
                {
                    return _backgroundImage;
                }
            }
        }

        private BitmapImage _screenImage;
        public BitmapImage ScreenImage
        {
            get
            {
                if (Screen == null)
                    return null;

                if (_screenImage == null)
                {
                    LoadScreenImage();

                    return _screenImage;
                }
                else
                {
                    return _screenImage;
                }
            }
        }

        public String DetailVisibility
        {
            get
            {
                if (_name == null)
                {
                    return "Collapsed";
                }
                else
                {
                    return "Visible";
                }
            }
        }

        public String LoadingStatusEpisode
        {
            get
            {
                if (_name == null)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public String LoadingStatusShouts
        {
            get
            {
                if (ShoutItems.Count == 0)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        #endregion

        public void LoadData(String tvdb, String season, String episode)
        {
            
            this._tvdb = tvdb;
            this._season = season;
            this._number = episode;

            String fileName = TraktWatched.getFolderStatic() + "/" + tvdb + season + episode + ".json";
            if (StorageController.doesFileExist(fileName))
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = false;
                worker.WorkerSupportsCancellation = false;
                worker.DoWork += new DoWorkEventHandler(episodeworker_DoWork);

                worker.RunWorkerAsync();

             
            }
            else
            {
                CallEpisodeService(tvdb, season, episode);
            }

   
        }

        void episodeworker_DoWork(object sender, DoWorkEventArgs e)
        {
            String fileName = TraktWatched.getFolderStatic() + "/" + _tvdb + _season + _number + ".json";
            TraktWatched episodeCache = (TraktWatched)StorageController.LoadObject(fileName, typeof(TraktWatched));
            if ((DateTime.Now - episodeCache.DownloadTime).Days < 1)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateEpisodeView(episodeCache);
                });
            }
            else
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    CallEpisodeService(_tvdb, _season, _number);
                });
            }
        }

        private void CallEpisodeService(String tvdb, String season, String episode)
        {
            var movieClient = new WebClient();
            movieClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadEpisodeStringCompleted);
            movieClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/summary.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + tvdb + "/" + season + "/" + episode), AppUser.createJsonStringForAuthentication());
        }

        void client_UploadEpisodeStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            String jsonString = e.Result;

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktWatched));
                TraktWatched episode = (TraktWatched)ser.ReadObject(ms);
                StorageController.saveObject(episode, typeof(TraktWatched));
                UpdateEpisodeView(episode);
                IsDataLoaded = true;
            }
        }

        private void UpdateEpisodeView(TraktWatched episode)
        {
            _showName = episode.Show.Title;
            _showYear = episode.Show.year;
            _name = episode.Episode.Title;
            _fanart = episode.Show.Images.Fanart;
            _overview = episode.Episode.Overview;
            _season = episode.Episode.Season;
            _number = episode.Episode.Number;
            _screen = episode.Episode.Images.Screen;
            _imdb = episode.Show.imdb_id;
            _airDate = episode.Episode.FirstAired;
            _watched = episode.Episode.Watched;
            if (episode.Episode.Ratings != null)
            {
                _rating = episode.Episode.Ratings.Percentage;
                _votes = episode.Episode.Ratings.Votes;
                _myRating = episode.Episode.MyRating;
                _myRatingAdvanced = episode.Episode.MyRatingAdvanced;
                NotifyPropertyChanged("RatingString");
            }
            NotifyPropertyChanged("Name");
            NotifyPropertyChanged("Fanart");
            NotifyPropertyChanged("Overview");
            NotifyPropertyChanged("Season");
            NotifyPropertyChanged("Number");
            NotifyPropertyChanged("AirDate");
            NotifyPropertyChanged("LoadingStatusEpisode");
            NotifyPropertyChanged("DetailVisibility");

            LoadBackgroundImage();
            LoadScreenImage();
        }

        private void LoadBackgroundImage()
        {
            String fileName = this._tvdb + "background" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                _backgroundImage = ImageController.getImageFromStorage(fileName);
                NotifyPropertyChanged("BackgroundImage");
            }
            else
            {
                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri(Fanart));
                request.BeginGetResponse(new AsyncCallback(request_OpenReadFanartCompleted), new object[] { request });
            }
        }

        private void LoadScreenImage()
        {
            String fileName = _imdb + _season + _number + "screenlarge" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                _screenImage = ImageController.getImageFromStorage(fileName);
                NotifyPropertyChanged("ScreenImage");
            }
            else
            {
                WebClient client = new WebClient();
                client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadScreenCompleted);
                client.OpenReadAsync(new Uri(Screen));

            }
        }

        void request_OpenReadFanartCompleted(IAsyncResult r)
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
                    _backgroundImage = ImageController.saveImage(_tvdb + "background.jpg", str, 800, 450, 100);
                    NotifyPropertyChanged("BackgroundImage");
                }));
            }
        }

        void client_OpenReadScreenCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            _screenImage = ImageController.saveImage(_imdb + _season + _number + "screenlarge.jpg", e.Result, 218, 123, 70);
            NotifyPropertyChanged("ScreenImage");

        }

        public void LoadShoutData(String tvdb, String season, String episode)
        {

             ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." });

            NotifyPropertyChanged("ShoutItems");

            var movieClient = new WebClient();
            
            this._tvdb = tvdb;
            movieClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadShoutStringCompleted);
            movieClient.DownloadStringAsync(new Uri("http://api.trakt.tv/show/episode/shouts.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + tvdb+ "/" + season + "/" + episode));
        }

        void client_DownloadShoutStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            String jsonString = e.Result;
            this.ShoutItems = new ObservableCollection<ListItemViewModel>();
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                TraktShout[] shouts = (TraktShout[])ser.ReadObject(ms);
                foreach(TraktShout shout in shouts)
                  this.ShoutItems.Add(new ListItemViewModel() { Name = shout.User.Username, ImageSource = shout.User.Avatar, Imdb = _imdb, SubItemText = shout.Shout });
            }

            if(this.ShoutItems.Count == 0)
                this.ShoutItems.Add(new ListItemViewModel() { Name = "No shouts" });
            ShoutsLoaded = true;
            NotifyPropertyChanged("ShoutItems");
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
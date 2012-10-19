using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using WPtrakt.Model.Trakt;
using WPtrakt.Model;
using VPtrakt.Controllers;
using System.Threading;
using System.Windows;

namespace WPtrakt
{
    public class ShowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ShoutItems { get; private set; }
        public ObservableCollection<ListItemViewModel> EpisodeItems { get;  set; }
        public Int16 numberOfSeasons { get; set; }
        public Int16 currentSeason {get; set;}
        public Boolean ShoutsLoaded { get; set; }

        public ShowViewModel()
        {
            ShoutsLoaded = false;
            ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." });
            this.EpisodeItems = new ObservableCollection<ListItemViewModel>();
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

        private Boolean _InWatchlist;
        public Boolean InWatchlist
        {
            get
            {
                return _InWatchlist;
            }
            set
            {
                if (value != _InWatchlist)
                {
                    _InWatchlist = value;

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

        private string _year;
        public string Year
        {
            get
            {
                return _year;
            }
            set
            {
                if (value != _year)
                {
                    _year = value;
                    NotifyPropertyChanged("Year");
                }
            }
        }

        private string _runtime;
        public string Runtime
        {
            get
            {
                return _runtime + " mins";
            }
            set
            {
                if (value != _runtime)
                {
                    _runtime = value;
                    NotifyPropertyChanged("Runtime");
                }
            }
        }

        private string _certification;
        public string Certification
        {
            get
            {
                return _certification;
            }
            set
            {
                if (value != _certification)
                {
                    _certification = value;
                    NotifyPropertyChanged("Certification");
                }
            }
        }

        private string[] _genres;
        public string[] Genres
        {
            get
            {
                return _genres;
            }
            set
            {
                if (value != _genres)
                {
                    _genres = value;
                    NotifyPropertyChanged("Genres");
                }
            }
        }

        public String GenreString
        {
            get
            {
                if (_genres == null)
                    return "";
                String genreString = "";
                foreach (String genre in _genres)
                {
                    genreString += genre + ", ";
                }
                if(!String.IsNullOrEmpty(genreString))
                genreString = genreString.Remove(genreString.Length - 2, 2);
                return genreString;
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

        public String LoadingStatusShow
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


        public String LoadingStatusSeason
        {
            get
            {
                if (EpisodeItems.Count == 0)
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

        public void LoadData(String tvdb)
        {
           
            if(currentSeason == 0)
             currentSeason = 1;
            this._tvdb = tvdb;

            String fileName = TraktShow.getFolderStatic() + "/" + tvdb + ".json";
            if (StorageController.doesFileExist(fileName))
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = false;
                worker.WorkerSupportsCancellation = false;
                worker.DoWork += new DoWorkEventHandler(showworker_DoWork);

                worker.RunWorkerAsync();
            }
            else
            {
                CallShowService(tvdb);
            }

            var seasonClient = new WebClient();
            seasonClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadSeasonsStringCompleted);
            seasonClient.DownloadStringAsync(new Uri("http://api.trakt.tv/show/seasons.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb));
        }

        void showworker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
            String fileName = TraktShow.getFolderStatic() + "/" + _tvdb + ".json";
            TraktShow show = (TraktShow)StorageController.LoadObject(fileName, typeof(TraktShow));
            if ((DateTime.Now - show.DownloadTime).Days < 1)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                 UpdateShowView(show);
                });
            }
            else
            {
                 System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                 {
                     CallShowService(_tvdb);
                 });
            }
        }

        private void CallShowService(String tvdb)
        {
            var showClient = new WebClient();
            showClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadShowStringCompleted);
            showClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb), AppUser.createJsonStringForAuthentication());
        }

        void client_UploadShowStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShow));
                    TraktShow show = (TraktShow)ser.ReadObject(ms);
                    StorageController.saveObject(show, typeof(TraktShow));
                    UpdateShowView(show);
                    IsDataLoaded = true;
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void UpdateShowView(TraktShow show)
        {
            _name = show.Title;
            _fanart = show.Images.Fanart;
            _genres = show.Genres;
            _imdb = show.imdb_id;
            _overview = show.Overview;
            _runtime = show.Runtime.ToString();
            _certification = show.Certification;
            _year = show.year.ToString();
            _InWatchlist = show.InWatchlist;
            _rating = show.Ratings.Percentage;
            _votes = show.Ratings.Votes;
            _myRating = show.MyRating;
            _myRatingAdvanced = show.MyRatingAdvanced;
            _watched = show.Watched;

            NotifyPropertyChanged("Name");
            NotifyPropertyChanged("Fanart");
            NotifyPropertyChanged("GenreString");
            NotifyPropertyChanged("Overview");
            NotifyPropertyChanged("Certification");
            NotifyPropertyChanged("Year");
            NotifyPropertyChanged("Runtime");
            NotifyPropertyChanged("LoadingStatusShow");
            NotifyPropertyChanged("DetailVisibility");
            NotifyPropertyChanged("RatingString");

            LoadBackgroundImage();
        }

        void client_DownloadSeasonsStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktSeason[]));
                    TraktSeason[] seasons = (TraktSeason[])ser.ReadObject(ms);
                    numberOfSeasons = 0;
                    foreach (TraktSeason season in seasons)
                    {
                        if (season.Season != "0")
                        {
                            numberOfSeasons += 1;
                        }
                    }
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
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

        public void LoadEpisodeData(String tvdb)
        {
            this.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            NotifyPropertyChanged("EpisodeItems");
            NotifyPropertyChanged("LoadingStatusSeason");
            if (numberOfSeasons > 0)
            {
                if (currentSeason <= numberOfSeasons)
                {
                    var showClient = new WebClient();
                    this._tvdb = tvdb;
                    showClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadEpisodeStringCompleted);
                    showClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/season.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb + "/" + currentSeason), AppUser.createJsonStringForAuthentication());
                }
            }
        }

        void client_UploadEpisodeStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktEpisode[]));
                    TraktEpisode[] episodes = (TraktEpisode[])ser.ReadObject(ms);

                    foreach (TraktEpisode episode in episodes)
                    {
                        this.EpisodeItems.Add(new ListItemViewModel() { Name = episode.Title, ImageSource = episode.Images.Screen, Imdb = _imdb + episode.Season + episode.Number, SubItemText = "Season " + episode.Season + ", Episode " + episode.Number, Episode = episode.Number, Season = episode.Season, Tvdb = _tvdb });
                    }
                    this.EpisodeItems.Add(new ListItemViewModel());

                }
                NotifyPropertyChanged("EpisodeItems");
                NotifyPropertyChanged("LoadingStatusSeason");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        public void LoadShoutData(String tvdb)
        {
            ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." });

            NotifyPropertyChanged("ShoutItems");

            var showClient = new WebClient();
            this._tvdb = tvdb;
            showClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadShoutStringCompleted);
            showClient.DownloadStringAsync(new Uri("http://api.trakt.tv/show/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb));
        }

        void client_DownloadShoutStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                this.ShoutItems = new ObservableCollection<ListItemViewModel>();

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                    TraktShout[] shouts = (TraktShout[])ser.ReadObject(ms);
                    foreach (TraktShout shout in shouts)
                        this.ShoutItems.Add(new ListItemViewModel() { Name = shout.User.Username, ImageSource = shout.User.Avatar, Imdb = _imdb, SubItemText = shout.Shout });
                }

                if (this.ShoutItems.Count == 0)
                    this.ShoutItems.Add(new ListItemViewModel() { Name = "No shouts" });
                ShoutsLoaded = true;
                NotifyPropertyChanged("ShoutItems");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
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
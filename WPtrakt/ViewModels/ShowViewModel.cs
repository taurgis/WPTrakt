using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;

namespace WPtrakt
{
    public class ShowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ShoutItems { get; private set; }
        public ObservableCollection<ListItemViewModel> EpisodeItems { get; set; }
        public Int16 numberOfSeasons { get; set; }
        public Int16 currentSeason { get; set; }
        public Boolean ShoutsLoaded { get; set; }

        public ShowViewModel()
        {
            ShoutsLoaded = false;
            ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." });
            this.EpisodeItems = new ObservableCollection<ListItemViewModel>();
        }

        #region Getters/Setters

        public List<String> SeasonItems
        {
            get
            {
                if (this.numberOfSeasons > 0)
                {
                    List<String> seasons = new List<string>();
                    for (int i = 1; i <= this.numberOfSeasons; i++)
                    {
                        seasons.Add(i.ToString());
                    }

                    return seasons;
                }
                else
                    return new List<string>();
            }

        }


        private string _name;
        public string Name
        {
            get
            {
                if (String.IsNullOrEmpty(_name))
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
                if (!String.IsNullOrEmpty(genreString))
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

            if (currentSeason == 0)
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

            String fileNameSeasons = TraktSeason.getFolderStatic() + "/" + tvdb + ".json";
            if (StorageController.doesFileExist(fileName))
            {
                BackgroundWorker seasonsWorker = new BackgroundWorker();
                seasonsWorker.WorkerReportsProgress = false;
                seasonsWorker.WorkerSupportsCancellation = false;
                seasonsWorker.DoWork += new DoWorkEventHandler(seasonsWorker_DoWork);

                seasonsWorker.RunWorkerAsync();
            }
            else
            {
                CallSeasonService(tvdb);
            }
        }

        void seasonsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            String fileName = TraktSeason.getFolderStatic() + "/" + _tvdb + ".json";
            TraktSeason[] seasons = (TraktSeason[])StorageController.LoadObjects(fileName, typeof(TraktSeason[]));

            if ((DateTime.Now - seasons[0].DownloadTime).Days < 7)
            {
                numberOfSeasons = 0;
                foreach (TraktSeason season in seasons)
                {
                    if (!String.IsNullOrEmpty(this.Tvdb))
                        season.Tvdb = this.Tvdb;

                    if (season.Season != "0")
                    {
                        numberOfSeasons += 1;
                    }
                }
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   NotifyPropertyChanged("SeasonItems");
               });
            }
            else
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    CallSeasonService(_tvdb);
                });
            }
        }


        void showworker_DoWork(object sender, DoWorkEventArgs e)
        {
            String fileName = TraktShow.getFolderStatic() + "/" + _tvdb + ".json";
            TraktShow show = (TraktShow)StorageController.LoadObject(fileName, typeof(TraktShow));

            if ((DateTime.Now - show.DownloadTime).Days < 7)
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

        private void CallSeasonService(String tvdb)
        {
            var seasonClient = new WebClient();
            seasonClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadSeasonsStringCompleted);
            seasonClient.DownloadStringAsync(new Uri("http://api.trakt.tv/show/seasons.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb));
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
                        if (!String.IsNullOrEmpty(this.Tvdb))
                            season.Tvdb = this.Tvdb;

                        if (season.Season != "0")
                        {
                            numberOfSeasons += 1;
                        }
                    }

                    seasons[0].DownloadTime = DateTime.Now;

                    if (!String.IsNullOrEmpty(this.Tvdb))
                        StorageController.saveObject(seasons, typeof(TraktSeason[]));
                }

                NotifyPropertyChanged("SeasonItems");
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
            try
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


                    HttpWebRequest largeRequest;

                    largeRequest = (HttpWebRequest)WebRequest.Create(new Uri(Fanart));
                    largeRequest.BeginGetResponse(new AsyncCallback(request_OpenReadFanartLargeCompleted), new object[] { largeRequest });
                }
            }
            catch (WebException) { }
        }

        void request_OpenReadFanartLargeCompleted(IAsyncResult r)
        {
            try
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
                       ImageController.saveImage(_tvdb + "largebackground.jpg", str, 1920, 100);
                    }));
                }
            }
            catch (WebException) { }
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
                    String fileNameEpisodes = TraktEpisode.getFolderStatic() + "/" + tvdb + currentSeason + ".json";
                    if (StorageController.doesFileExist(fileNameEpisodes))
                    {
                        BackgroundWorker episodesWorker = new BackgroundWorker();
                        episodesWorker.WorkerReportsProgress = false;
                        episodesWorker.WorkerSupportsCancellation = false;
                        episodesWorker.DoWork += new DoWorkEventHandler(episodesWorker_DoWork);

                        episodesWorker.RunWorkerAsync();
                    }
                    else
                    {
                        CallEpisodesService(tvdb);
                    }
                }
            }
        }

        void episodesWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            String fileName = TraktEpisode.getFolderStatic() + "/" + this.Tvdb + this.currentSeason + ".json";
            TraktEpisode[] episodes = (TraktEpisode[])StorageController.LoadObjects(fileName, typeof(TraktEpisode[]));

            if ((DateTime.Now - episodes[0].DownloadTime).Days < 7)
            {
                foreach (TraktEpisode episodeIt in episodes)
                {
                    episodeIt.Tvdb = this.Tvdb;
                    TraktEpisode episode = episodeIt;
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                     {
                         this.EpisodeItems.Add(new ListItemViewModel() { Name = episode.Title, ImageSource = episode.Images.Screen, Imdb = _imdb + episode.Season + episode.Number, SubItemText = "Season " + episode.Season + ", Episode " + episode.Number, Episode = episode.Number, Season = episode.Season, Tvdb = _tvdb, Watched = episode.Watched, Rating = episode.MyRatingAdvanced, InWatchList = episode.InWatchlist });
                        });
                }
                
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.EpisodeItems.Add(new ListItemViewModel());
                    NotifyPropertyChanged("EpisodeItems");
                    NotifyPropertyChanged("LoadingStatusSeason");
                });
            }
            else
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    CallSeasonService(_tvdb);
                });
            }
        }

        private void CallEpisodesService(String tvdb)
        {
            var showClient = new WebClient();
            this._tvdb = tvdb;
            showClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadEpisodeStringCompleted);
            showClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/season.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb + "/" + currentSeason), AppUser.createJsonStringForAuthentication());
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
                        episode.Tvdb = this.Tvdb;
                        this.EpisodeItems.Add(new ListItemViewModel() { Name = episode.Title, ImageSource = episode.Images.Screen, Imdb = _imdb + episode.Season + episode.Number, SubItemText = "Season " + episode.Season + ", Episode " + episode.Number, Episode = episode.Number, Season = episode.Season, Tvdb = _tvdb, Watched = episode.Watched, Rating = episode.MyRatingAdvanced, InWatchList = episode.InWatchlist });
                    }
                    episodes[0].DownloadTime = DateTime.Now;

                    DateTime airTime = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                    airTime = airTime.AddSeconds(episodes[episodes.Length-1].FirstAired);

                   int daysSinceRelease = (DateTime.Now - airTime).Days;

                    this.EpisodeItems.Add(new ListItemViewModel());



                    if (!String.IsNullOrEmpty(this.Tvdb) && !(daysSinceRelease < 30))
                        StorageController.saveObject(episodes, typeof(TraktEpisode[]));

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
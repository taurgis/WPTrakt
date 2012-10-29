using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using VPtrakt.Controllers;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;

namespace WPtrakt
{
    public class MovieViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ShoutItems { get; private set; }
        public Boolean ShoutsLoaded { get; set; }

        public MovieViewModel()
        {
            ShoutsLoaded = false;
            ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." }); 
        }

        #region Getters/Setters

        public bool IsDataLoaded
        {
            get;
            private set;
        }

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

        private string _tmdb;
        public string Tmdb
        {
            get
            {
                return _tmdb;
            }
            set
            {
                if (value != _tmdb)
                {
                    _tmdb = value;
                    NotifyPropertyChanged("Tmdb");
                }
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
                    NotifyPropertyChanged("MyRatingAdvanced");
                    NotifyPropertyChanged("RatingString");
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
                    NotifyPropertyChanged("InWatchlist");
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
                    NotifyPropertyChanged("GenreString");
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
                    NotifyPropertyChanged("Rating");
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
                    NotifyPropertyChanged("Watched");
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
                    NotifyPropertyChanged("Votes");
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
                        baseString += " - Mine: " + this.MyRatingAdvanced+"/10";
                    }
                }
                return baseString;
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

        private string _trailer;
        public string Trailer
        {
            get
            {
                return _trailer;
            }
            set
            {
                if (value != _trailer)
                {
                    _trailer = value;
                    NotifyPropertyChanged("Trailer");
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

        public String LoadingStatusMovie
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

        #endregion

        public void LoadData(String imdbId)
        {
            this._imdb = imdbId;
            String fileName = TraktMovie.getFolderStatic() + "/" + _imdb + ".json";
            
         
            if (StorageController.doesFileExist(fileName))
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = false;
                worker.WorkerSupportsCancellation = false;
                worker.DoWork += new DoWorkEventHandler(movieworker_DoWork);

                worker.RunWorkerAsync();
            }
            else
            {
               CallMovieService(imdbId);
            }
        }

        void movieworker_DoWork(object sender, DoWorkEventArgs e)
        {
            String fileName = TraktMovie.getFolderStatic() + "/" + _imdb + ".json";
            TraktMovie movie = (TraktMovie)StorageController.LoadObject(fileName, typeof(TraktMovie));
            if ((DateTime.Now - movie.DownloadTime).Days < 7)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   UpdateMovieView(movie);
               });
            }
            else
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   CallMovieService(_imdb);
               });
            }
        }

        private void CallMovieService(String imdbId)
        {
            var movieClient = new WebClient();
            this._imdb = imdbId;
            movieClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMovieStringCompleted);
            movieClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + imdbId), AppUser.createJsonStringForAuthentication());
        }

        void client_UploadMovieStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie));
                    TraktMovie movie = (TraktMovie)ser.ReadObject(ms);
                    StorageController.saveObject(movie, typeof(TraktMovie));
                    UpdateMovieView(movie);
                 
                    IsDataLoaded = true;
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void UpdateMovieView(TraktMovie movie)
        {
            this.Name = movie.Title;
            this.Fanart = movie.Images.Fanart;
            this.Genres = movie.Genres;
            this.Overview = movie.Overview;
            this.Runtime = movie.Runtime.ToString();
            this.Certification = movie.Certification;
            this.Year = movie.year.ToString();
            this.InWatchlist = movie.InWatchlist;
            this.Rating = movie.Ratings.Percentage;
            this.Votes = movie.Ratings.Votes;
            this.MyRating = movie.MyRating;
            this.MyRatingAdvanced = movie.MyRatingAdvanced;
            this.Watched = movie.Watched;
            this.Imdb = movie.imdb_id;
            this.Tmdb = movie.Tmdb;
            this.Trailer = movie.Trailer;

            NotifyPropertyChanged("LoadingStatusMovie");
            NotifyPropertyChanged("DetailVisibility");

            LoadBackgroundImage();
        }

        public void LoadShoutData(String imdbId)
        {
            ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." });

            NotifyPropertyChanged("ShoutItems");

            var movieClient = new WebClient();
            this._imdb = imdbId;
            movieClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadShoutStringCompleted);
            movieClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + imdbId), AppUser.createJsonStringForAuthentication());
        }

        void client_UploadShoutStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                ShoutsLoaded = true;
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

                NotifyPropertyChanged("ShoutItems");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void LoadBackgroundImage()
        {
            String fileName = this._imdb + "background" + ".jpg";

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
                   _backgroundImage = ImageController.saveImage(_imdb + "background.jpg", str, 800, 450, 100);

                   NotifyPropertyChanged("BackgroundImage");
               }));
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
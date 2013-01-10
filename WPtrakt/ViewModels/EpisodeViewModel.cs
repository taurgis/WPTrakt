using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public class EpisodeViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ShoutItems { get; private set; }
        public Boolean ShoutsLoading { get; set; }

        public EpisodeViewModel()
        {
            ShoutsLoading = false;
            ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." });
        }

        #region Getters/Setters

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
                    NotifyPropertyChanged("ShowName");
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
                    NotifyPropertyChanged("ShowYear");
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
                    NotifyPropertyChanged("Watched");
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
                    NotifyPropertyChanged("Rating");
                }
            }
        }

        private Int16 _year;
        public Int16 Year
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

                return _backgroundImage;

            }
            set
            {

                _backgroundImage = value;
                NotifyPropertyChanged("BackgroundImage");

            }
        }

        private BitmapImage _screenImage;
        public BitmapImage ScreenImage
        {
            get
            {

                return _screenImage;

            }
            set
            {

                _screenImage = value;
                NotifyPropertyChanged("ScreenImage");

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
       
        public void UpdateEpisodeView(TraktEpisode episode, TraktShow show)
        {
            this.ShowName = episode.Title;
            this.ShowYear = show.year;
            this.Name = episode.Title;
            this.Fanart = show.Images.Fanart;
            this.Overview = episode.Overview;
            this.Season = episode.Season;
            this.Number = episode.Number;
            this.Screen = episode.Images.Screen;
            this.Imdb = show.imdb_id;
            this._airDate = episode.FirstAired;
            this.Watched = episode.Watched;
            this.InWatchlist = episode.InWatchlist;
            this.Year = show.year;

            if (episode.Ratings != null)
            {
                this.Rating = episode.Ratings.Percentage;
                this.Votes = episode.Ratings.Votes;
                this.MyRating = episode.MyRating;
                this.MyRatingAdvanced = episode.MyRatingAdvanced;
                NotifyPropertyChanged("RatingString");
            }

            NotifyPropertyChanged("LoadingStatusEpisode");
            NotifyPropertyChanged("DetailVisibility");
        }

        /*
       

        

        public void LoadShoutData(String tvdb, String season, String episode)
        {

            ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." });

            NotifyPropertyChanged("ShoutItems");

            var movieClient = new WebClient();

            this._tvdb = tvdb;
            movieClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadShoutStringCompleted);
            movieClient.DownloadStringAsync(new Uri("http://api.trakt.tv/show/episode/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb + "/" + season + "/" + episode));
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
                    {
                        this.ShoutItems.Add(new ListItemViewModel() { Name = shout.User.Username, ImageSource = shout.User.Avatar, Imdb = _imdb, SubItemText = shout.Shout });
                    }

                    ms.Close();
                }

                if (this.ShoutItems.Count == 0)
                    this.ShoutItems.Add(new ListItemViewModel() { Name = "No shouts" });
                ShoutsLoading = true;
                NotifyPropertyChanged("ShoutItems");
            }
            catch (WebException) { ErrorManager.ShowConnectionErrorPopup(); }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

        }
        */
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
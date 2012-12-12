using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public class MovieViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ShoutItems { get; private set; }
        public Boolean ShoutsLoading { get; set; }

        public MovieViewModel()
        {
            ShoutsLoading = false;
            this.ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." }); 
        }

        public void clearShouts()
        {
            this.ShoutItems = new ObservableCollection<ListItemViewModel>();
            NotifyPropertyChanged("ShoutItems");
        }

        public void addShout(ListItemViewModel model)
        {
            if (this.ShoutItems == null)
                this.ShoutItems = new ObservableCollection<ListItemViewModel>();

            this.ShoutItems.Add(model);
            NotifyPropertyChanged("ShoutItems");
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
                int count = 1;

                if (_genres == null)
                    return "";
                String genreString = "";
                foreach (String genre in _genres)
                {
                    if(count++ <= 3)
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
                return _backgroundImage;
            }
            set
            {
                _backgroundImage = value;

                NotifyPropertyChanged("BackgroundImage");
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

        public void UpdateMovieView(TraktMovie movie)
        {
            this.Name = movie.Title;
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
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtraktBase.Controller;
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

        public String WallPaperImage { get; set; }

        private BitmapImage _wallPaper;
        public BitmapImage WallPaper
        {
            get
            {
                if (String.IsNullOrEmpty(WallPaperImage))
                    return null;

                if (_wallPaper == null)
                {
                    LoadWallpaparImage();

                    return null;
                }
                else
                {
                    return _wallPaper;
                }
            }

            set
            {
                _wallPaper = value;

                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged("WallPaper");
                }));
            }
        }

        private async void LoadWallpaparImage()
        {
            this.WallPaper = await ShowController.getLargeCoverImage(this.Imdb, this.WallPaperImage);
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
                    NotifyPropertyChanged("RatingImage");
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

        public String VotesString
        {
            get
            {
                return this.Votes + " votes";
            }
        }

        public String RatingString
        {
            get
            {
                String baseString;
                baseString = this.Rating + "%";

               

                return baseString;
            }
        }

        public Uri AllRatingImage
        {
            get
            {
                if (this.Rating < 60)
                    return new Uri("Images/icon-hate-large.png", UriKind.Relative);
                else
                    return new Uri("Images/icon-love-large.png", UriKind.Relative);
            }
        }


        public Uri RatingImage
        {
            get
            {
                if (!(this.MyRatingAdvanced == 0))
                    return new Uri("Images/badge-" + this.MyRatingAdvanced + ".png", UriKind.Relative);
                else
                    return null;
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
            this.WallPaperImage = movie.Images.Poster;
            this.Name = movie.Title;
            this.Genres = movie.Genres;
            this.Overview = movie.Overview;
            this.Runtime = movie.Runtime.ToString();
            this.Certification = movie.Certification;
            this.Year = movie.year.ToString();
            this.InWatchlist = movie.InWatchlist;
            this.Rating = movie.Ratings.Percentage;
            this.Votes = movie.Ratings.Votes;
            this.MyRatingAdvanced = movie.MyRatingAdvanced;
            this.Watched = movie.Watched;
            this.Imdb = movie.imdb_id;
            this.Tmdb = movie.Tmdb;
            this.Trailer = movie.Trailer;

            NotifyPropertyChanged("LoadingStatusMovie");
            NotifyPropertyChanged("DetailVisibility");
            NotifyPropertyChanged("WallPaper");
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
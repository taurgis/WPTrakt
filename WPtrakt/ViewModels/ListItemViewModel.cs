using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;

namespace WPtrakt
{
    public class ListItemViewModel : INotifyPropertyChanged
    {
        private string _movieName;
        public string Name
        {
            get
            {
                if (TruncateTitle)
                {
                    if (_movieName.Length > 17)
                        return _movieName.Substring(0, 17) + "...";
                }
                return _movieName;
            }
            set
            {
                if (value != _movieName)
                {
                    _movieName = value;
                    NotifyPropertyChanged("Name");
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
                _rating = value;
                NotifyPropertyChanged("Rating");
                NotifyPropertyChanged("RatingVisibility");
                NotifyPropertyChanged("RatingImage");
            }
        }
        private String _numberOfRatings;
        public String NumberOfRatings
        {
            get
            {
                return "( " + _numberOfRatings + " )";
            }
            set
            {
                _numberOfRatings = value;
                NotifyPropertyChanged("NumberOfRatings");
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
                _watched = value;
                this.NotifyPropertyChanged("SeenVisibility");
                this.NotifyPropertyChanged("ContextMarkAsWatchedVisibility");
                this.NotifyPropertyChanged("ContextUnMarkAsWatchedVisibility");
            }
        }

        private Boolean _inWatchList;
        public Boolean InWatchList
        {
            get
            {
                return _inWatchList;
            }
            set
            {
                _inWatchList = value;
                this.NotifyPropertyChanged("InWatchList");
                this.NotifyPropertyChanged("WatchlistVisibility");
                this.NotifyPropertyChanged("ContextWatchlist");
                this.NotifyPropertyChanged("ContextUnWatchlist");
            }
        }

        public String SeenVisibility
        {
            get
            {
                if (this.Watched)
                    return "Visible";
                else
                    return "Collapsed";
            }
           
        }

        public Int16 Year { get; set; }

        public String ContextMarkAsWatchedVisibility
        {
            get
            {
                if (!this.Watched)
                    return "Visible";
                else
                    return "Collapsed";
            }

        }


        public String ContextWatchlist
        {
            get
            {
                if (!this.InWatchList)
                    return "Visible";
                else
                    return "Collapsed";
            }

        }

        public String ContextUnWatchlist
        {
            get
            {
                if (this.InWatchList)
                    return "Visible";
                else
                    return "Collapsed";
            }

        }

        public String ContextUnMarkAsWatchedVisibility
        {
            get
            {
                if (this.Watched)
                    return "Visible";
                else
                    return "Collapsed";
            }
        }

        public String RatingVisibility
        {
            get
            {
                if (this.Rating > 0)
                    return "Visible";
                else
                    return "Collapsed";
            }

        }

        public String WatchlistVisibility
        {
            get
            {
                if (this.InWatchList)
                    return "Visible";
                else
                    return "Collapsed";
            }

        }

        public Uri RatingImage
        {
            get
            {
                if (this.Rating > 0)
                    return new Uri("Images/badge-" + this.Rating + ".png", UriKind.Relative);
                else
                    return new Uri("Images/badge-1.png", UriKind.Relative) ;
            }

        }

        private Double _watchedCompletion;
        public Double WatchedCompletion
        {
            get
            {
                return _watchedCompletion;
            }
            set
            {
                this._watchedCompletion = value;
                NotifyPropertyChanged("WatchedCompletion");
            }
        }

        public long WatchedTime { get; set; }

        public String WatchedDate
        {
            get
            {
                if (WatchedTime > 0)
                {
                    DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                    return baseTime.AddSeconds(WatchedTime).ToString("dd/MM") + " " + baseTime.AddSeconds(WatchedTime).ToShortTimeString();
                }
                else
                    return "Currently Watching";
            }
        }

        public String WatchedVisible
        {
            get
            {
                if (Watched)
                    return "156";
                else
                    return "0";
            }
        }

        public String SeasonText
        {
            get
            {
                return "Season " + this._season + ", Episode " + _episode;
            }
        }

        private Boolean _truncateTitle;

        public Boolean TruncateTitle
        {
            get
            {
  
                return _truncateTitle;
            }
            set
            {
                if (value != _truncateTitle)
                {
                    _truncateTitle = value;
                }
            }
        }

        private String _episode;
        public String Episode
        {
            get
            {
                return _episode;
            }
            set
            {
                if (value != _episode)
                {
                    _episode = value;
                }
            }
        }

        private String _season;
        public String Season
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
                }
            }
        }


        private String _year;
        public String SubItemText
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
                    NotifyPropertyChanged("SubItemText");
                }
            }
        }
        private string _imageSource;

        public string ImageSource
        {
            get
            {
                return _imageSource;
            }
            set
            {
                _imageSource = value;
            }
        }


        public string ImageSourceMedium
        {
            get
            {
                if (_type.Equals("Movie"))
                {
                    return _imageSource.Replace(".2.jpg", ".1-300.jpg");
                }
                else
                  return _imageSource.Replace(".2.jpg", "-138.2.jpg");
            }
        }

        private String _type;
        public String Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
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
                _imdb = value;
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
                _tvdb = value;
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
                int count = 1;
                foreach (String genre in _genres)
                {
                    if(count++ <= 3)
                        genreString += genre + ", ";
                }
                if (!String.IsNullOrEmpty(genreString))
                    genreString = genreString.Remove(genreString.Length - 2, 2);
                return genreString;
            }
        }

        private BitmapImage _screenImage;
        public BitmapImage ScreenImage
        {
            get
            {
                if (String.IsNullOrEmpty(ImageSource))
                    return null;

                if (_screenImage == null)
                {
                    LoadScreenImage();
                    
                    return null;
                }
                else
                {
                    return _screenImage;
                }
            }

            set
            {
                _screenImage = value;

                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged("ScreenImage");
                }));
            }
        }

        private async void LoadScreenImage()
        { 
           this.ScreenImage = await ShowController.getSmallScreenImage(this.Imdb, this.Season, this.Episode, this.ImageSource); 
        }

        private BitmapImage _largeScreenImage;
        public BitmapImage LargeScreenImage
        {
            get
            {
                if (String.IsNullOrEmpty(ImageSource))
                    return null;

                if (_largeScreenImage == null)
                {
                    LoadLargeScreenImage();
                   // BitmapImage tempImage = new BitmapImage(new Uri("Images/screen-small.jpg", UriKind.Relative));
                    return null;
                }
                else
                {
                    return _largeScreenImage;
                }
            }

            set
            {
                _largeScreenImage = value;

                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged("LargeScreenImage");
                }));
            }
        }

        private async void LoadLargeScreenImage()
        {
            this.LargeScreenImage = await ShowController.getLargeScreenImageStatic(this.Imdb, this.Season, this.Episode, this.ImageSource);
        }

        private BitmapImage _mediumImage;
        public BitmapImage MediumImage
        {
            get
            {
                if (_mediumImage == null)
                {
                    LoadMediumImage();
                    return null;
                }
                else
                {
                    return _mediumImage;
                }
            }

            set
            {
                this._mediumImage = value;

                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged("MediumImage");
                }));
            }
        }

        private async void LoadMediumImage()
        {
            this.MediumImage = await ShowController.getMediumCoverImage(this._imdb, _imageSource.Replace(".2.jpg", "-138.2.jpg"));
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


        public static string GetCaptionGroup(ListItemViewModel model)
        {
            char key = char.ToLower(model.Name[0]);

            if (key < 'a' || key > 'z')
            {
                key = '#';
            }

            return key.ToString();
        }
    }
}
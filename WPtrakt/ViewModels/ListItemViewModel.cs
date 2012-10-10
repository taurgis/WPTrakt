using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using System.Threading;

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
            }
        }

        public String WatchedVisible
        {
            get
            {
                if (Watched)
                    return "133";
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

        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>




        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        /// 
        private BitmapImage _thumbImage;
        public BitmapImage ThumbImage
        {
            get
            {
                if (_thumbImage == null)
                {
                    String fileName = this._imdb + "thumb" + ".jpg";

                    if (StorageController.doesFileExist(fileName))
                    {
                        _thumbImage = ImageController.getImageFromStorage(fileName);
                    }
                    else
                    {
                        try
                        {
                            WebClient client = new WebClient();
                            client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadThumbCompleted);
                            client.OpenReadAsync(new Uri(_imageSource.Replace(".2.jpg", "-138.2.jpg")));
                        }
                        catch (NullReferenceException) { }
                        return null;
                    }

                    return _thumbImage;
                }
                else
                {
                    return _thumbImage;
                }
            }
        }

        private BitmapImage _screenImage;
        public BitmapImage ScreenImage
        {
            get
            {
                if (_screenImage == null)
                {
                    String fileName = this._imdb + "screen" + ".jpg";

                    if (StorageController.doesFileExist(fileName))
                    {
                        _screenImage = ImageController.getImageFromStorage(fileName);
                    }
                    else
                    {
                        if (_imageSource != null)
                        {
                            WebClient client = new WebClient();
                            client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadScreenCompleted);
                            client.OpenReadAsync(new Uri(_imageSource));
                        }
                        return null;
                    }

                    return _screenImage;
                }
                else
                {
                    return _screenImage;
                }
            }
        }

        private BitmapImage _mediumImage;
        public BitmapImage MediumImage
        {
            get
            {
                if (_mediumImage == null)
                {
                    String fileName = this._imdb + "medium" + ".jpg";

                    if (StorageController.doesFileExist(fileName))
                    {
                        _mediumImage = ImageController.getImageFromStorage(fileName);
                    }
                    else
                    {
                        WebClient client = new WebClient();
                        client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadMediumCompleted);
                        client.OpenReadAsync(new Uri(_imageSource.Replace(".2.jpg", "-138.2.jpg")));
                        return null;
                    }
                    return _mediumImage;  
                }
                else
                {
                    return _mediumImage;
                }
            }
        }

        void client_OpenReadMediumCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            _mediumImage = ImageController.saveImage(_imdb + "medium.jpg", e.Result, 120, 179, 70);
            NotifyPropertyChanged("MediumImage");

        }
        void client_OpenReadScreenCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            _screenImage = ImageController.saveImage(_imdb + "screen.jpg", e.Result, 100, 56, 90);
            NotifyPropertyChanged("ScreenImage");
        }

        void client_OpenReadThumbCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            _thumbImage = ImageController.saveImage(_imdb + "thumb.jpg", e.Result, 61, 91, 70);
            NotifyPropertyChanged("ThumbImage");
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
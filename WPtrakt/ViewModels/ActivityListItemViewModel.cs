using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Model;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;

namespace WPtrakt.ViewModels
{
    public class ActivityListItemViewModel  : INotifyPropertyChanged
    {
        public String Imdb { get; set; }
        public String Tvdb { get; set; }
        public String Type { get; set; }
        public Int16 Season { get; set; }
        public Int16 Episode { get; set; }
        public Int16 Year { get; set; }

        public bool hasBeenUnrealized { get; set; }
        public bool IsAlmostLastDay { get; set; }

        private String _name;
        public String Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private DateTime _date;
        public DateTime Date
        {
            get
            {
                return _date;
            }
            set
            {
                if (value != _date)
                {
                    _date = value;
                    NotifyPropertyChanged("Date");
                }
            }
        }


        public String Header
        {
            get
            {
                if (this.Date.Day == DateTime.Now.Day && this.Date.Month == DateTime.Now.Month && this.Date.Year == DateTime.Now.Year)
                {
                    return "Today";
                }
                else if (this.Date.Day == DateTime.Now.AddDays(-1).Day && this.Date.Month == DateTime.Now.Month && this.Date.Year == DateTime.Now.Year)
                {
                    return "Yesterday";
                }
                else
                    return this.Date.ToShortDateString();

            }
        }

        public Boolean HasHeader { get; set; }

        public String HeaderVisibility
        {
            get
            {
                if (HasHeader)
                    return "Visible";
                else
                    return "Collapsed";
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

        public String ScreenVisibility
        {
            get
            {
                if (AppUser.Instance.SmallScreenshotsEnabled || (AppUser.Instance.ImagesWithWIFI && StorageController.IsConnectedToWifi()))
                {
                    return "Visible";
                }

                else
                {
                    return "Collapsed";
                }
            }
        }

        public String ScreenVisibilityFiller
        {
            get
            {
                if (AppUser.Instance.SmallScreenshotsEnabled || (AppUser.Instance.ImagesWithWIFI && StorageController.IsConnectedToWifi()))
                {
                    return "Collapsed";
                }

                else
                {
                    return "Visible";
                }
            }
        }

        private BitmapImage _screenImage;
        public BitmapImage ScreenImage
        {
            get
            {
                if (!AppUser.Instance.SmallScreenshotsEnabled && !(AppUser.Instance.ImagesWithWIFI && StorageController.IsConnectedToWifi()))
                    return null;

                if (String.IsNullOrEmpty(Screen))
                    return null;

                if (_screenImage == null)
                {
                    if(this.Type == "movie" || this.Type == "show")
                        return new BitmapImage(new Uri("Images/poster-small.jpg", UriKind.RelativeOrAbsolute));
                    else
                        return new BitmapImage(new Uri("Images/screen-small.jpg", UriKind.RelativeOrAbsolute));
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

        public async void LoadScreenImage()
        {
            if (_screenImage == null)
            {
                if (!String.IsNullOrEmpty(this.Imdb))
                    this.ScreenImage = await ShowController.getSmallScreenImage(this.Imdb, this.Season.ToString(), this.Episode.ToString(), (this.Screen.Contains("poster-dark.jpg") ? this.Screen : this.Screen.Replace(".jpg", "-138.jpg")), 138);
                else
                    this.ScreenImage = await ShowController.getSmallScreenImage(this.Tvdb, this.Season.ToString(), this.Episode.ToString(), (this.Screen.Contains("/fanart/") ? this.Screen.Replace("-940.86.jpg", "-218.jpg") : this.Screen.Replace(".jpg", "-218.jpg")), 120);
            }
        }

        private String _activity;
        public String Activity
        {
            get
            {
                return _activity;
            }

            set
            {
                if (_activity != value)
                {
                    _activity = value;
                    NotifyPropertyChanged("Activity");
                }
            }
        }



        private Int32 _timeStamp;
        public Int32 TimeStamp
        {
            get
            {
                return _timeStamp;
            }

            set
            {
                if (_timeStamp != value)
                {
                    _timeStamp = value;
                    NotifyPropertyChanged("TimeStamp");
                }
            }
        }

        public String DateTimeString
        {
            get
            {
                if (_timeStamp > 0)
                {
                    DateTime time = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                    time = time.AddSeconds(_timeStamp);
                    return time.ToLocalTime().ToShortDateString() + " " + time.ToLocalTime().ToShortTimeString();
                }
                else
                    return "";
            }
        }

        private String _time;
        public String Time
        {
            get
            {
                return _time;
            }

            set
            {
                if (_time != value)
                {
                    _time = value;
                    NotifyPropertyChanged("Time");
                }
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

        internal void ClearScreenImage()
        {
            this._screenImage = null;
        }

       
    }
}
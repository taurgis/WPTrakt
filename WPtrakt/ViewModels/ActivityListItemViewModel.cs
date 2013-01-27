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

        private BitmapImage _screenImage;
        public BitmapImage ScreenImage
        {
            get
            {
                if (String.IsNullOrEmpty(Screen))
                    return null;

                if (_screenImage == null)
                {
                    LoadScreenImage();
                    BitmapImage tempImage = new BitmapImage(new Uri("Images/screen-small.jpg", UriKind.Relative));
                    return tempImage;
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
            this.ScreenImage = await ShowController.getSmallScreenImage(this.Imdb, this.Season.ToString(), this.Episode.ToString(), this.Screen);
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

        public String DateTime
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
    }
}
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
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
                if (_screenImage == null)
                {
                     String fileName = "";
                     if (!String.IsNullOrEmpty(Imdb) && this.Season == 0)
                     {
                         fileName = this.Imdb + "screen" + ".jpg";
                     }
                     else
                     {
                         if (this.Season > 0)
                         {
                             fileName = this.Tvdb + Season + Episode + "screen" + ".jpg";
                         }
                         else
                         {
                             fileName = this.Tvdb + "screen" + ".jpg";
                         }
                     }

                    if (StorageController.doesFileExist(fileName))
                    {
                        _screenImage = ImageController.getImageFromStorage(fileName);
                    }
                    else
                    {
                        if (Screen != null)
                        {
                            HttpWebRequest request;

                            request = (HttpWebRequest)WebRequest.Create(new Uri(Screen));
                            request.BeginGetResponse(new AsyncCallback(request_OpenReadScreenCompleted), new object[] { request });

                            BitmapImage tempImage = new BitmapImage(new Uri("Images/screen-small.jpg", UriKind.Relative));
                            return tempImage;
                        }
                    }

                    return _screenImage;
                }
                else
                {
                    return _screenImage;
                }
            }
        }

        void request_OpenReadScreenCompleted(IAsyncResult r)
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
                        String fileName;
                        if (!String.IsNullOrEmpty(Imdb) && this.Season == 0)
                        {
                            fileName = this.Imdb + "screen" + ".jpg";
                        }
                        else
                        {
                            if (this.Season > 0)
                            {
                                fileName = this.Tvdb + Season + Episode + "screen" + ".jpg";
                            }
                            else
                            {
                                fileName = this.Tvdb + "screen" + ".jpg";
                            }
                        }
                        _screenImage = ImageController.saveImage(fileName, str, 100, 100);

                        NotifyPropertyChanged("ScreenImage");
                    }));
                }
            }
            catch (WebException) { }
            catch (TargetInvocationException) { }

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
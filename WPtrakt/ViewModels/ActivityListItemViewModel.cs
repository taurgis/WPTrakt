using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;

namespace WPtrakt.ViewModels
{
    public class ActivityListItemViewModel  : INotifyPropertyChanged
    {
        public String Imdb { get; set; }
        public String Tvdb { get; set; }
        public String Type { get; set; }
        public Int16 Season { get; set; }
        public Int16 Episode { get; set; }

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

        private String _avatar;
        public String Avatar
        {
            get
            {
                return _avatar;
            }

            set
            {
                if (_avatar != value)
                {
                    _avatar = value;
                    NotifyPropertyChanged("Avatar");
                }
            }
        }

        private BitmapImage _avatarImage;
        public BitmapImage AvatarImage
        {
            get
            {
                if (_avatarImage == null)
                {
                    String fileName = this.Name + "thumb" + ".jpg";

                    if (StorageController.doesFileExist(fileName))
                    {
                        _avatarImage = ImageController.getImageFromStorage(fileName);
                    }
                    else
                    {
                        try
                        {
                            HttpWebRequest request;

                            request = (HttpWebRequest)WebRequest.Create(new Uri(this.Avatar));
                            request.BeginGetResponse(new AsyncCallback(request_OpenReadThumbCompleted), new object[] { request });
                        }
                        catch (NullReferenceException) { }
                        return null;
                    }

                    return _avatarImage;
                }
                else
                {
                    return _avatarImage;
                }
            }
        }

        private void request_OpenReadThumbCompleted(IAsyncResult r)
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
                    _avatarImage = ImageController.saveImage(this.Name + "thumb.jpg", str, 50, 50, 70);
                    NotifyPropertyChanged("AvatarImage");
                }));
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

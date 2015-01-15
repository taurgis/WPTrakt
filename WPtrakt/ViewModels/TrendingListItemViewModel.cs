using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Model;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;

namespace WPtrakt.ViewModels
{
    public class TrendingListItemViewModel  : INotifyPropertyChanged
    {
        public String Imdb { get; set; }
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
        public string ImageSource
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
                    NotifyPropertyChanged("ImageSource");
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

        private BitmapImage _screenImage;
        public BitmapImage ScreenImage
        {
            get
            {
                if (!AppUser.Instance.SmallScreenshotsEnabled && !(AppUser.Instance.ImagesWithWIFI && StorageController.IsConnectedToWifi()))
                    return null;

                if (String.IsNullOrEmpty(ImageSource))
                    return null;

                if (_screenImage == null)
                {
                    return new BitmapImage(new Uri("Images/poster-small.jpg", UriKind.RelativeOrAbsolute));
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
            this.ScreenImage = await ShowController.getSmallScreenImage(this.Imdb, "0", "0", (this.ImageSource.Contains("poster-dark.jpg") ? this.ImageSource : this.ImageSource.Replace(".jpg", "-138.jpg")), 138);
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
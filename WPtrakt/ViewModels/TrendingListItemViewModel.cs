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
        public BitmapImage Image
        {
            get
            {
                if (!AppUser.Instance.SmallScreenshotsEnabled && !(AppUser.Instance.ImagesWithWIFI && StorageController.IsConnectedToWifi()))
                    return null;

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
                    NotifyPropertyChanged("Image");
                }));
            }
        }

        private async void LoadScreenImage()
        {
            this.Image = await ShowController.getMediumCoverImage(this.Imdb, this.ImageSource);
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
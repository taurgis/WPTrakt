using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;

namespace WPtrakt
{
    public class TrendingViewModel : INotifyPropertyChanged
    {
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


        private Int32 _width;
        public Int32 ImageWidth
        {
            get
            {
                return (_width);
            }
            set
            {
                this._width = value;
                NotifyPropertyChanged("Width");
            }
        }

        private BitmapImage _trendingImage;
        public BitmapImage TrendingImage
        {
            get
            {
                if (_trendingImage == null)
                {
                    LoadTrendingImage();
                    return null;
                }
                else
                {
                    return _trendingImage;
                }
            }

            set
            {
                this._trendingImage = value;

                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged("TrendingImage");
                }));
            }
        }

        private async void LoadTrendingImage()
        {
            this.TrendingImage = await ShowController.getTrendingImage(_imageSource, (short)(this._width));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            try
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (null != handler)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            catch (Exception) { }
        }




    }
}
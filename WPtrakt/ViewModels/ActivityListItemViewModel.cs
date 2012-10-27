using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;

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

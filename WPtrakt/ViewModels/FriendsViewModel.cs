﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtraktBase.Model.Trakt;


namespace WPtrakt
{
    public class FriendsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ResultItems { get; private set; }
        public ObservableCollection<ListItemViewModel> FollowingResultItems { get; private set; }
        public ObservableCollection<ListItemViewModel> FollowersResultItems { get; private set; }
        public FriendsViewModel()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        internal void clearResult()
        {
            this.ResultItems = new ObservableCollection<ListItemViewModel>();
            this.FollowersResultItems = new ObservableCollection<ListItemViewModel>();
            this.FollowingResultItems = new ObservableCollection<ListItemViewModel>();
            NotifyPropertyChanged("ResultItems");
            NotifyPropertyChanged("FollowersResultItems");
            NotifyPropertyChanged("FollowingResultItems");
        }


    }
}
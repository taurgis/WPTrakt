using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using WPtrakt.Controllers;
using WPtrakt.Custom;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;


namespace WPtrakt
{
    public class MyMoviesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AlphaKeyGroup<ListItemViewModel>> MovieItems { get; private set; }
        public ObservableCollection<ListItemViewModel> SuggestItems { get; private set; }
        private Boolean LoadingSuggestItems { get; set; }
        private Boolean LoadingMovies { get; set; }

        private MovieController controller;

        public ProgressIndicator Indicator { get; set; }


        public MyMoviesViewModel()
        {
            this.MovieItems = new ObservableCollection<AlphaKeyGroup<ListItemViewModel>>();
            this.SuggestItems = new ObservableCollection<ListItemViewModel>();
            this.controller = new MovieController();

        }

        #region Getters/Setters

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public String LoadingStatus
        {
            get
            {
                if (this.LoadingMovies || this.LoadingSuggestItems)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        #endregion

        #region MyMovies

        public void LoadData()
        {
            if (!this.LoadingMovies)
            {
                this.LoadingMovies = true;
                this.MovieItems = new ObservableCollection<AlphaKeyGroup<ListItemViewModel>>();
                RefreshMyMoviesView();
               
                LoadMovies();

                this.IsDataLoaded = true;
            }
        }

        private async void LoadMovies()
        {
            TraktMovie[] myMovies = await controller.getUserMovies();

            ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();

            foreach (TraktMovie movie in myMovies)
            {
                tempItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres });
            }

            if (tempItems.Count == 0)
            {
                tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
            }

            this.LoadingMovies = false;

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
           {
               this.Indicator.IsVisible = false;
               UpdateMyMovieView(AlphaKeyGroup<ListItemViewModel>.CreateGroups(tempItems, Thread.CurrentThread.CurrentUICulture, (ListItemViewModel s) => { return s.Name; }, true));
           });
        }

        private void UpdateMyMovieView(ObservableCollection<AlphaKeyGroup<ListItemViewModel>> tempItems)
        {
            this.MovieItems = tempItems;
            RefreshMyMoviesView();
        }

        private void RefreshMyMoviesView()
        {
            NotifyPropertyChanged("MovieItems");
            NotifyPropertyChanged("LoadingStatus");
        }

        public void FilterMovies(int type)
        {
             if (type == 0)
            {
                this.LoadData();
            }
            else if (type == 1)
            {
                this.LoadMyWatchListMoviesData();
            }
        }

        #endregion

        #region MyWatchListMovies

        public void LoadMyWatchListMoviesData()
        {
            if (!this.LoadingMovies)
            {
                this.LoadingMovies = true;
                this.MovieItems = new ObservableCollection<AlphaKeyGroup<ListItemViewModel>>();
                RefreshMyMoviesView();
                LoadWatchlist();
            }
        }

        private async void LoadWatchlist()
        {
            TraktMovie[] myMovies = await controller.getUserWatchlist();

            ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();

            foreach (TraktMovie movie in myMovies)
            {
                tempItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres });
            }

            if (tempItems.Count == 0)
            {
                tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
            }

            this.LoadingMovies = false;

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.Indicator.IsVisible = false;
                UpdateMyMovieView(AlphaKeyGroup<ListItemViewModel>.CreateGroups(tempItems, Thread.CurrentThread.CurrentUICulture, (ListItemViewModel s) => { return s.Name; }, true));
            });

        }

        

        #endregion

        #region Suggest

        public void LoadSuggestData()
        {
            if (!this.LoadingSuggestItems)
            {
                this.LoadingSuggestItems = true;
                this.SuggestItems = new ObservableCollection<ListItemViewModel>();
                NotifyPropertyChanged("SuggestItems");
                NotifyPropertyChanged("LoadingStatus");
                LoadSuggestions();
            }
        }


        private async void LoadSuggestions()
        {
            TraktMovie[] obj = await controller.getUserSuggestions();
            int counter = 1;
            this.SuggestItems = new ObservableCollection<ListItemViewModel>();

            foreach (TraktMovie movie in obj)
            {
                if (counter++ > 8)
                    break;

                this.SuggestItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, Type = "Movie", InWatchList = movie.InWatchlist, SubItemText = movie.year.ToString() });
            }


            LoadingSuggestItems = false;
            this.Indicator.IsVisible = false;
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NotifyPropertyChanged("SuggestItems");
                NotifyPropertyChanged("LoadingStatus");
            });
        }

        #endregion

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
﻿
using Microsoft.Phone.Controls;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public partial class MyMovies : PhoneApplicationPage
    {
        public MyMovies()
        {
            InitializeComponent();
            DataContext = App.MyMoviesViewModel;
            this.Loaded += new RoutedEventHandler(MyMoviesPage_Loaded);

            this.Filter.SelectedIndex = AppUser.Instance.MyMoviesFilter;
        }

        private void MyMoviesPage_Loaded(object sender, RoutedEventArgs e)
        {

            if (!App.MyMoviesViewModel.IsDataLoaded && App.MyMoviesViewModel.MovieItems.Count == 0)
            {
                App.MyMoviesViewModel.Indicator = App.ShowLoading(this); 
                if (this.Filter.SelectedIndex == 0)
                {
                    this.AllText.Visibility = System.Windows.Visibility.Visible;
                    this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
                    App.MyMoviesViewModel.LoadData();
                }
                else if (this.Filter.SelectedIndex == 1)
                {
                    this.AllText.Visibility = System.Windows.Visibility.Collapsed;
                    this.WatchlistText.Visibility = System.Windows.Visibility.Visible;
                    App.MyMoviesViewModel.LoadMyWatchListMoviesData();
                }
            }
        }

        #region Taps

        private void Canvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
             ListItemViewModel model = (ListItemViewModel)((Canvas)sender).DataContext;
             Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Image)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        private void AllText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.Filter.Open();
        }

        #endregion

        #region ApplicationBar

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            App.TrackEvent("MyMovies", "Switched panorama to " + this.MyMoviesPanorama.SelectedIndex);
            if (this.MyMoviesPanorama.SelectedIndex == 0)
            {
                App.MyMoviesViewModel.Indicator = App.ShowLoading(this); 
                if (this.Filter.SelectedIndex == 0)
                {
                    App.MyMoviesViewModel.LoadData();
                }
                else if (this.Filter.SelectedIndex == 1)
                {
                    App.MyMoviesViewModel.LoadMyWatchListMoviesData();
                }
            }
            else
            {
               App.MyMoviesViewModel.Indicator = App.ShowLoading(this); 
               App.MyMoviesViewModel.LoadSuggestData();
            }
        }

        #endregion

        #region MovieContextMenu

        private ListItemViewModel lastModel;

        private void SeenMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;
            var seenClient = new WebClient();

            seenClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMovieSeenStringCompleted);
            WatchedAuth auth = new WatchedAuth();
            auth.Movies = new TraktMovieRequest[1];
            auth.Movies[0] = new TraktMovieRequest();
            auth.Movies[0].imdb_id = lastModel.Imdb;
            auth.Movies[0].Title = lastModel.Name;
            auth.Movies[0].year = Int16.Parse(lastModel.SubItemText);
            auth.Movies[0].Plays = 1;

            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            auth.Movies[0].LastPlayed = (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;

            seenClient.UploadStringAsync(new Uri("https://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
        }

        private void client_UploadMovieSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                lastModel.Watched = true;
                ToastNotification.ShowToast("Movie", "Movie marked as watched.");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

            lastModel = null;
        }

        private void WatchlistMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;
            var watchlistClient = new WebClient();
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMovieWatchlistStringCompleted);
            WatchlistAuth auth = new WatchlistAuth();
            auth.Movies = new TraktMovie[1];
            auth.Movies[0] = new TraktMovie();
            auth.Movies[0].imdb_id = lastModel.Imdb;
            auth.Movies[0].Title = lastModel.Name;
            auth.Movies[0].year = Int16.Parse(lastModel.SubItemText);
            watchlistClient.UploadStringAsync(new Uri("https://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadMovieWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                lastModel.InWatchList = true;
                ToastNotification.ShowToast("Movie", "Movie added to watchlist.");

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

            lastModel = null;
        }

        private void CheckinMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            var checkinClient = new WebClient();
            checkinClient.UploadStringCompleted += new UploadStringCompletedEventHandler(checkinClient_UploadStringCompleted);
            CheckinAuth auth = new CheckinAuth();

            auth.imdb_id = lastModel.Imdb;
            auth.Title = lastModel.Name;
            auth.year = Int16.Parse(lastModel.SubItemText);
            auth.AppDate = AppUser.getReleaseDate();

            var assembly = Assembly.GetExecutingAssembly().FullName;
            var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];
            auth.AppVersion = fullVersionNumber;

            checkinClient.UploadStringAsync(new Uri("https://api.trakt.tv/movie/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));
        }

        void checkinClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                if (jsonString.Contains("failure"))
                    ToastNotification.ShowToast("Movie", "There is already a checkin in progress.");
                else
                    ToastNotification.ShowToast("Movie", "Checked in!");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

            lastModel = null;
        }

        #endregion

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MyMoviesPanorama.SelectedIndex == 1)
            {
                if (App.MyMoviesViewModel.SuggestItems.Count == 0)
                {
                    App.MyMoviesViewModel.Indicator = App.ShowLoading(this); 
                    App.MyMoviesViewModel.LoadSuggestData();
                }
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                ListSuggestions.Width = 700;

            }
            else
            {
                ListSuggestions.Width = 1370;
            }
        }
        private Int32 lastSelection;
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Filter != null)
            {
                if (this.lastSelection != this.Filter.SelectedIndex)
                {
                    App.MyMoviesViewModel.Indicator = App.ShowLoading(this); 
                    this.lastSelection = this.Filter.SelectedIndex;
                    AppUser.Instance.MyMoviesFilter = this.Filter.SelectedIndex;

                    if (this.Filter.SelectedIndex == 0)
                    {
                        this.AllText.Visibility = System.Windows.Visibility.Visible;
                        this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else if (this.Filter.SelectedIndex == 1)
                    {
                        this.AllText.Visibility = System.Windows.Visibility.Collapsed;
                        this.WatchlistText.Visibility = System.Windows.Visibility.Visible;
                    }

                    App.MyMoviesViewModel.FilterMovies(this.Filter.SelectedIndex);


                }
            }
        }

    }
}
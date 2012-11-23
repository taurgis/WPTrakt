using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Scheduler;
using System;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtrakt.ViewModels;

namespace WPtrakt
{
    public partial class Main : PhoneApplicationPage
    {
        public Main()
        {
            InitializeComponent();
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    ToastNotification.ShowToast("Connection", "No connection available!");
                    return;
                }
                var assembly = Assembly.GetExecutingAssembly().FullName;
                var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];

                if (!App.ViewModel.IsDataLoaded)
                {
                    ReloadLiveTile();
                    if (String.IsNullOrEmpty(AppUser.Instance.AppVersion) && (String.IsNullOrEmpty(AppUser.Instance.UserName) || String.IsNullOrEmpty(AppUser.Instance.Password)))
                    {
                        AppUser.Instance.AppVersion = fullVersionNumber;
                        NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
                    }
                    else
                    {
                        if (AppUser.Instance.AppVersion != fullVersionNumber)
                        {
                            MessageBox.Show("Application update. Clearing cache, the application will hang for a few seconds.");
                            AppUser.ClearCache();
                            AppUser.Instance.AppVersion = fullVersionNumber;
                        }

                        App.ViewModel.LoadData();
                    }
                }
            }
            catch (InvalidOperationException) { }
        }

        private static void ReloadLiveTile()
        {
            try
            {
               
                if (AppUser.Instance.LiveTileEnabled)
                {
                    var taskName = "WPtraktLiveTile";

                    // If the task exists
                    var oldTask = ScheduledActionService.Find(taskName) as PeriodicTask;
                    if (oldTask != null)
                    {
                        ScheduledActionService.Remove(taskName);
                    }

                    // Create the Task
                    PeriodicTask task = new PeriodicTask(taskName);

                    // Description is required
                    task.Description = "This task updates the WPtrakt live tile.";

                    // Add it to the service to execute
                    ScheduledActionService.Add(task);
                    //ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(3));

                }
            }
            catch (InvalidOperationException) { }
        }

        private void MainPanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPanorama.SelectedIndex == 1)
            {
                if (App.ViewModel.TrendingItems.Count == 0)
                {
                    App.ViewModel.loadTrending();
                }
            }
            else if (MainPanorama.SelectedIndex == 2)
            {
                if (App.ViewModel.HistoryItems.Count == 0)
                {
                    App.ViewModel.LoadHistoryData();
                }
            }
        }

        #region Taps

        private void ApplicationBarRefreshButton_Click(object sender, EventArgs e)
        {
            if (MainPanorama.SelectedIndex == 0)
            {
                StorageController.DeleteFile(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json");
                App.ViewModel.LoadData();
            }
            else if (MainPanorama.SelectedIndex == 1)
            {
                App.ViewModel.loadTrending();
            }
            else if (MainPanorama.SelectedIndex == 2)
            {
                App.ViewModel.LoadHistoryData();
            }
        }

        private void MyMovies(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot,  new Uri("/MyMovies.xaml", UriKind.Relative));
        }

        private void MyShows_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/MyShows.xaml", UriKind.Relative));
        }

        private void Search_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/Search.xaml", UriKind.Relative));
        }

        private void TrendingImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (lastModel == null)
            {
                ListItemViewModel model = (ListItemViewModel)((Image)sender).DataContext;
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
            }
        }

        private void Grid_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ActivityListItemViewModel model = (ActivityListItemViewModel)((Grid)sender).DataContext;

            Uri redirectUri = null;
            if (model.Type.Equals("episode"))
                redirectUri = new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative);
            else if (model.Type.Equals("movie"))
                redirectUri = new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative);
            else
                redirectUri = new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative);

            Animation.NavigateToFadeOut(this, LayoutRoot, redirectUri);
        }

        #endregion

        #region Menu

        private void CancelCheckin_Click(object sender, EventArgs e)
        {
            var cancelCheckinClient = new WebClient();

            cancelCheckinClient.UploadStringCompleted += new UploadStringCompletedEventHandler(cancelCheckinClient_UploadStringCompleted);
            cancelCheckinClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/cancelcheckin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication());
        }

        void cancelCheckinClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                ToastNotification.ShowToast("Cancel", "Cancelled any active check in!");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void FriendActivity_Click(object sender, EventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/FriendActivity.xaml", UriKind.Relative));
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

            seenClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
        }

        private void client_UploadMovieSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                lastModel.Watched = true;
                MessageBox.Show("Movie marked as watched.");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
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
            auth.Movies[0].imdb_id =lastModel.Imdb;
            auth.Movies[0].Title = lastModel.Name;
            auth.Movies[0].year = Int16.Parse(lastModel.SubItemText);
            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadMovieWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                lastModel.InWatchList = true;
                MessageBox.Show("Movie added to watchlist.");

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
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

            checkinClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));
        }

        void checkinClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                if (jsonString.Contains("failure"))
                    MessageBox.Show("There is already a checkin in progress.");
                else
                    MessageBox.Show("Checked in!");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null;
        }

        #endregion

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                this.MainPanorama.Margin = new Thickness(0, 0, 0, 0);
                ListTrending.Width = 700;
                HistoryList.Height = 520;
            }
            else
            {
                if (e.Orientation == PageOrientation.LandscapeRight)
                {
                    this.MainPanorama.Margin = new Thickness(40, -180, 0, 0);
                }
                else
                {
                    this.MainPanorama.Margin = new Thickness(0, -180, 0, 0);
                }

                ListTrending.Width = 1370;
                HistoryList.Height = 400;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }


    }
}
using System;
using System.IO.IsolatedStorage;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using WPtrakt.Controllers;
using WPtrakt.Model.Trakt.Request;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;

namespace WPtrakt
{
    public partial class ViewMovie : PhoneApplicationPage
    {
        public ViewMovie()
        {
            InitializeComponent();

            DataContext = App.MovieViewModel;
            this.Loaded += new RoutedEventHandler(ViewMovie_Loaded);
        }

        private void ViewMovie_Loaded(object sender, RoutedEventArgs e)
        {
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.MovieViewModel.LoadData(id);
        }

        private void MoviePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MoviePanorama.SelectedIndex == 1)
            {
                if (!App.MovieViewModel.ShoutsLoaded)
                {
                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    App.MovieViewModel.LoadShoutData(id);
                }
                InitAppBarShouts();
            }
            else
            {
                InitAppBar();
            }

        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.MovieViewModel = null;
            Animation.FadeOut(LayoutRoot);
        }

        #region Taps

        private void ImdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri( "http://www.imdb.com/title/" + App.MovieViewModel.Imdb);

            task.Show();
        }

        private void TmdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://www.themoviedb.org/movie/" + App.MovieViewModel.Tmdb);

            task.Show();
        }

        private void TrailerButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
   

            Regex Youtube = new Regex("youtu(?:\\.be|be\\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");

            Match youtubeMatch = Youtube.Match(App.MovieViewModel.Trailer);

            string id = string.Empty;

            if (youtubeMatch.Success)
                id = youtubeMatch.Groups[1].Value;

            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri("http://www.youtube.com/embed/" + id +"?autoplay=1");
            webBrowserTask.Show();
        }

        #endregion 

        #region AppBar

        private void ListBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitAppBar();
        }

        private void InitAppBar()
        {
            ApplicationBar appBar = new ApplicationBar();

            if (!App.MovieViewModel.InWatchlist)
                CreateAddToWatchlist(appBar);
            else
                CreateRemoveFromWatchlist(appBar); ;

            CreateRatingButton(appBar);

            if (App.MovieViewModel.Watched)
                CreateUnSeenButton(appBar);
            else
                CreateSeenButton(appBar);

            CreateCheckingButton(appBar);

            CreateTileMenuItem(appBar);

            this.ApplicationBar = appBar;
        }

        private void CreateTileMenuItem(ApplicationBar appBar)
        {
            ApplicationBarMenuItem tileMenuItem = new ApplicationBarMenuItem();
            tileMenuItem.Text = "Pin to start";
            tileMenuItem.Click += new EventHandler(tileMenuItem_Click);
            appBar.MenuItems.Add(tileMenuItem);
        }

        void tileMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (StorageController.doesFileExist(App.MovieViewModel.Imdb + "background.jpg"))
                {
                    ImageController.copyImageToShellContent(App.MovieViewModel.Imdb + "background.jpg", App.MovieViewModel.Imdb);
                    FlipTileData NewTileData = new FlipTileData
                    {
                        BackgroundImage =
                             new Uri("isostore:/Shared/ShellContent/wptraktbg" + App.MovieViewModel.Imdb + ".jpg", UriKind.Absolute),
                        WideBackgroundImage =
                      new Uri("isostore:/Shared/ShellContent/wptraktbg" + App.MovieViewModel.Imdb + ".jpg", UriKind.Absolute),
                        Title = App.MovieViewModel.Name,
                    };

                    ShellTile.Create(
                    new Uri(
                        "/ViewMovie.xaml?id=" + App.MovieViewModel.Imdb,
                        UriKind.Relative),
                        NewTileData, true);
                }
            }
            catch (InvalidOperationException) { ToastNotification.ShowToast("Tile", "Error creating tile, please try again!"); }
        }


        private void CreateRatingButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton ratingButton = new ApplicationBarIconButton();
            ratingButton = new ApplicationBarIconButton(new Uri("Images/appbar.favs.rest.png", UriKind.Relative));
            ratingButton.IsEnabled = true;
            ratingButton.Text = "Rate";
            ratingButton.Click += new EventHandler(RateClick);

            appBar.Buttons.Add(ratingButton);
        }

        private void RateClick(object sender, EventArgs e)
        {
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktMovie.getFolderStatic() + "/" + App.MovieViewModel.Imdb + ".json");
    
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=movie&imdb=" + App.MovieViewModel.Imdb + "&year=" + App.MovieViewModel.Year + "&title=" + App.MovieViewModel.Name, UriKind.Relative));
        }

        private void CreateRemoveFromWatchlist(ApplicationBar appBar)
        {
            ApplicationBarIconButton removeFromWatchlist = new ApplicationBarIconButton();
            removeFromWatchlist = new ApplicationBarIconButton(new Uri("Images/appbar.feature.removevideo.rest.png", UriKind.Relative));
            removeFromWatchlist.Text = "Watchlist -";
            removeFromWatchlist.Click += new EventHandler(removeFromWatchlist_Click);
            appBar.Buttons.Add(removeFromWatchlist);
        }

        void removeFromWatchlist_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadRemoveFromWatchlistStringCompleted);

            WatchlistAuth auth = new WatchlistAuth();
            auth.Movies = new TraktMovie[1];
            auth.Movies[0] = new TraktMovie();
            auth.Movies[0].imdb_id = App.MovieViewModel.Imdb;
            auth.Movies[0].Title = App.MovieViewModel.Name;
            auth.Movies[0].year = Int16.Parse(App.MovieViewModel.Year);

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadRemoveFromWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Movie", "Movie removed from watchlist.");
                App.MovieViewModel.InWatchlist = false;
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktMovie.getFolderStatic() + "/" + App.MovieViewModel.Imdb + ".json");
                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void CreateAddToWatchlist(ApplicationBar appBar)
        {
            ApplicationBarIconButton enabledAddtoWatchlist = new ApplicationBarIconButton();

            enabledAddtoWatchlist = new ApplicationBarIconButton(new Uri("Images/appbar.feature.video.rest.png", UriKind.Relative));
            enabledAddtoWatchlist.IsEnabled = true;
            enabledAddtoWatchlist.Click += new EventHandler(AddToWatchList_Click);
            enabledAddtoWatchlist.Text = "Watchlist +";
            appBar.Buttons.Add(enabledAddtoWatchlist);
        }

        private void AddToWatchList_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadWatchlistStringCompleted);
            WatchlistAuth auth = new WatchlistAuth();
            auth.Movies = new TraktMovie[1];
            auth.Movies[0] = new TraktMovie();
            auth.Movies[0].imdb_id = App.MovieViewModel.Imdb;
            auth.Movies[0].Title = App.MovieViewModel.Name;
            auth.Movies[0].year = Int16.Parse(App.MovieViewModel.Year);
            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                App.MovieViewModel.InWatchlist = true;
                ToastNotification.ShowToast("Movie", "Movie added to watchlist.");

                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktMovie.getFolderStatic() + "/" + App.MovieViewModel.Imdb + ".json");
                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void CreateCheckingButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton checkinButton = new ApplicationBarIconButton();
            checkinButton = new ApplicationBarIconButton(new Uri("Images/appbar.check.rest.png", UriKind.Relative));
            checkinButton.Text = "Check In";
            checkinButton.Click += new EventHandler(checkinButton_Click);

            appBar.Buttons.Add(checkinButton);
        }

        void checkinButton_Click(object sender, EventArgs e)
        {
            var checkinClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            checkinClient.UploadStringCompleted += new UploadStringCompletedEventHandler(checkinClient_UploadStringCompleted);
            CheckinAuth auth = new CheckinAuth();

            auth.imdb_id = App.MovieViewModel.Imdb;
            auth.Title = App.MovieViewModel.Name;
            auth.year = Int16.Parse(App.MovieViewModel.Year);
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
                    ToastNotification.ShowToast("Movie", "There is already a checkin in progress.");
                else
                    ToastNotification.ShowToast("Movie", "Checked in!");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ShoutsIconButton_Click(object sender, EventArgs e)
        {
            App.MovieViewModel.LoadShoutData(App.MovieViewModel.Imdb);
        }

    
        private void CreateSeenButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(SeenClick);

            appBar.Buttons.Add(watchedButton);
        }

        private void SeenClick(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadSeenStringCompleted);
            WatchedAuth auth = new WatchedAuth();
            auth.Movies = new TraktMovieRequest[1];
            auth.Movies[0] = new TraktMovieRequest();
            auth.Movies[0].imdb_id = App.MovieViewModel.Imdb;
            auth.Movies[0].Title = App.MovieViewModel.Name;
            auth.Movies[0].year = Int16.Parse(App.MovieViewModel.Year);
            auth.Movies[0].Plays = 1;

            DateTime UnixEpoch =  new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            auth.Movies[0].LastPlayed = (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
        }

        void client_UploadSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Movie", "Movie marked as watched.");
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktMovie.getFolderStatic() + "/" + App.MovieViewModel.Imdb + ".json");
                App.MovieViewModel.Watched = true;
                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }


        private void CreateUnSeenButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton unseeButton = new ApplicationBarIconButton();
            unseeButton = new ApplicationBarIconButton(new Uri("Images/appbar.unseen.rest.png", UriKind.Relative));
            unseeButton.Text = "Seen";
            unseeButton.Click += new EventHandler(unseeButton_Click);

            appBar.Buttons.Add(unseeButton);
        }

        void unseeButton_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadUnSeenStringCompleted);
            WatchedAuth auth = new WatchedAuth();
            auth.Movies = new TraktMovieRequest[1];
            auth.Movies[0] = new TraktMovieRequest();
            auth.Movies[0].imdb_id = App.MovieViewModel.Imdb;
            auth.Movies[0].Title = App.MovieViewModel.Name;
            auth.Movies[0].year = Int16.Parse(App.MovieViewModel.Year);
            auth.Movies[0].Plays = 1;

            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            auth.Movies[0].LastPlayed = (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
        }

        void client_UploadUnSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Movie", "Movie unmarked as watched.");
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktMovie.getFolderStatic() + "/" + App.MovieViewModel.Imdb + ".json");
                App.MovieViewModel.Watched = false;
                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        #endregion

        #region Shouts AppBar

        private void InitAppBarShouts()
        {
            ApplicationBar appBar = new ApplicationBar();

            CreateRefreshShoutsButton(appBar);
            CreateSendButton(appBar);
            this.ApplicationBar = appBar;
        }

        private void CreateRefreshShoutsButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.refresh.rest.png", UriKind.Relative));
            watchedButton.Text = "Refresh";
            watchedButton.Click += new EventHandler(ShoutsIconButton_Click);

            appBar.Buttons.Add(watchedButton);
        }


        private void CreateSendButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton sendButton = new ApplicationBarIconButton();
            sendButton = new ApplicationBarIconButton(new Uri("Images/appbar.send.text.rest.png", UriKind.Relative));
            sendButton.IsEnabled = true;
            sendButton.Text = "Send";
            sendButton.Click += new EventHandler(sendButton_Click);

            appBar.Buttons.Add(sendButton);
        }

        void sendButton_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty((ShoutText.Text)))
            {
                var watchlistClient = new WebClient();
                watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadShoutStringCompleted);
                ShoutAuth auth = new ShoutAuth();

                auth.Imdb = App.MovieViewModel.Imdb;
                auth.Title = App.MovieViewModel.Name;
                auth.Year = Int16.Parse(App.MovieViewModel.Year);

                auth.Shout = (ShoutText.Text);

                watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/shout/movie/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
            }
        }

      
        void client_UploadShoutStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Movie", "Shout posted.");

                ShoutText.Text = "";

                this.Focus();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
        }

        #endregion

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                this.MoviePanorama.Margin = new Thickness(0, 0, 0, 0);
                ShoutList.Width = 405;
                ShoutList.Height = 420;
            }
            else
            {
                this.MoviePanorama.Margin = new Thickness(0, -180, 0, 0);
                ShoutList.Width = 800;
                ShoutList.Height = 340;
            }

            MoviePanorama.DefaultItem = MoviePanorama.Items[0];
        }

    }
}
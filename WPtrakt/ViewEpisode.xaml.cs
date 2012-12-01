using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.IO.IsolatedStorage;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WPtrakt.Controllers;
using WPtrakt.Model.Trakt.Request;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;

namespace WPtrakt
{
    public partial class ViewEpisode : PhoneApplicationPage
    {
        public ViewEpisode()
        {
            InitializeComponent();
    
            DataContext = App.EpisodeViewModel;
            this.Loaded += new RoutedEventHandler(ViewEpisode_Loaded);
        }

        private void ViewEpisode_Loaded(object sender, RoutedEventArgs e)
        {
            String id;
            String season;
            String episode;
            NavigationContext.QueryString.TryGetValue("id", out id);
            NavigationContext.QueryString.TryGetValue("season", out season);
            NavigationContext.QueryString.TryGetValue("episode", out episode);
            App.EpisodeViewModel.LoadData(id, season, episode);
            LayoutRoot.Opacity = 1;
        }

        private void EpisodePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.EpisodePanorama.SelectedIndex == 1)
            {
                if (!App.EpisodeViewModel.ShoutsLoaded)
                {
                    String id;
                    String season;
                    String episode;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    NavigationContext.QueryString.TryGetValue("season", out season);
                    NavigationContext.QueryString.TryGetValue("episode", out episode);
                    App.EpisodeViewModel.LoadShoutData(id, season, episode);
                }

                InitAppBarShouts();

            }
            else
            {
                InitAppBar();
            }
        }

        #region Taps

        private void ImdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri( "http://www.imdb.com/title/" + App.EpisodeViewModel.Imdb);

            task.Show();
        }

        private void TvdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://thetvdb.com/?tab=series&id=" + App.EpisodeViewModel.Tvdb);

            task.Show();
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

            if (!App.EpisodeViewModel.InWatchlist)
                CreateAddToWatchlist(appBar);
            else
                CreateRemoveFromWatchlist(appBar); ;

            CreateRatingButton(appBar);
            if (App.EpisodeViewModel.Watched)
                CreateUnSeenButton(appBar);
            else
                CreateSeenButton(appBar);

            CreateCheckingButton(appBar);

            CreateBackToShowMenuItem(appBar);

            this.ApplicationBar = appBar;
        }

        private void CreateSeenButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(Seen_Click);

            appBar.Buttons.Add(watchedButton);
        }

        private void Seen_Click(object sender, EventArgs e)
        {
            var seenClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            seenClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadSeenStringCompleted);
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = App.EpisodeViewModel.Season;
            auth.Episodes[0].Episode = App.EpisodeViewModel.Number;
            auth.Imdb = App.EpisodeViewModel.Imdb;
            auth.Title = App.EpisodeViewModel.ShowName;
            auth.Year = App.EpisodeViewModel.ShowYear;

            seenClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));
        }

        void client_UploadSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                ToastNotification.ShowToast("Episode", "Episode marked as watched.");
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktWatched.getFolderStatic() + "/" + App.EpisodeViewModel.Tvdb + App.EpisodeViewModel.Season + App.EpisodeViewModel.Number + ".json");

                App.EpisodeViewModel.Watched = true;
                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
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
            var unseeClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            unseeClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadUnSeenStringCompleted);
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = App.EpisodeViewModel.Season;
            auth.Episodes[0].Episode = App.EpisodeViewModel.Number;
            auth.Imdb = App.EpisodeViewModel.Imdb;
            auth.Title = App.EpisodeViewModel.ShowName;
            auth.Year = App.EpisodeViewModel.ShowYear;

            unseeClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));
        }

        void client_UploadUnSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                App.EpisodeViewModel.Watched = false;
                ToastNotification.ShowToast("Episode", "Episode unmarked as watched.");

                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktWatched.getFolderStatic() + "/" + App.EpisodeViewModel.Tvdb + App.EpisodeViewModel.Season + App.EpisodeViewModel.Number + ".json");

                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }


        private void CreateRatingButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton ratingButton = new ApplicationBarIconButton();
            ratingButton = new ApplicationBarIconButton(new Uri("Images/appbar.favs.rest.png", UriKind.Relative));
            ratingButton.IsEnabled = true;
            ratingButton.Text = "Rate";
            ratingButton.Click += new EventHandler(ratingButton_Click);

            appBar.Buttons.Add(ratingButton);
        }

        private void ratingButton_Click(object sender, EventArgs e)
        {
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktWatched.getFolderStatic() + "/" + App.EpisodeViewModel.Tvdb + App.EpisodeViewModel.Season + App.EpisodeViewModel.Number  + ".json");
           
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=episode&imdb=" + App.EpisodeViewModel.Imdb + "&year=" + App.EpisodeViewModel.ShowYear + "&title=" + App.EpisodeViewModel.ShowName + "&season=" + App.EpisodeViewModel.Season + "&episode=" + App.EpisodeViewModel.Number, UriKind.Relative));
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
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = App.EpisodeViewModel.Season;
            auth.Episodes[0].Episode = App.EpisodeViewModel.Number;
            auth.Imdb = App.EpisodeViewModel.Imdb;
            auth.Title = App.EpisodeViewModel.ShowName;
            auth.Year = App.EpisodeViewModel.ShowYear;

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));
        }

        void client_UploadWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                App.EpisodeViewModel.InWatchlist = true;

                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktWatched.getFolderStatic() + "/" + App.EpisodeViewModel.Tvdb + App.EpisodeViewModel.Season + App.EpisodeViewModel.Number + ".json");

                ToastNotification.ShowToast("Episode", "Episode added to watchlist.");

                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
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

            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = App.EpisodeViewModel.Season;
            auth.Episodes[0].Episode = App.EpisodeViewModel.Number;
            auth.Imdb = App.EpisodeViewModel.Imdb;
            auth.Title = App.EpisodeViewModel.ShowName;
            auth.Year = App.EpisodeViewModel.ShowYear;

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));
        }

        void client_UploadRemoveFromWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Episode", "Episode removed from watchlist.");
                App.EpisodeViewModel.InWatchlist = false;

                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktWatched.getFolderStatic() + "/" + App.EpisodeViewModel.Tvdb + App.EpisodeViewModel.Season + App.EpisodeViewModel.Number + ".json");

                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
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

            auth.tvdb_id = App.EpisodeViewModel.Tvdb;
            auth.Title = App.EpisodeViewModel.ShowName;
            auth.year = App.EpisodeViewModel.Year;
            auth.Season = Int16.Parse(App.EpisodeViewModel.Season);
            auth.Episode = Int16.Parse(App.EpisodeViewModel.Number);
            auth.AppDate = AppUser.getReleaseDate();

            var assembly = Assembly.GetExecutingAssembly().FullName;
            var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];
            auth.AppVersion = fullVersionNumber;

            checkinClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));
        }

        void checkinClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                if (jsonString.Contains("failure"))
                    ToastNotification.ShowToast("Episode", "There is already a checkin in progress.");
                else
                    ToastNotification.ShowToast("Episode", "Checked in!");
                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void CreateBackToShowMenuItem(ApplicationBar appBar)
        {
            ApplicationBarMenuItem backtoShowMenuItem = new ApplicationBarMenuItem();
            backtoShowMenuItem.Text = "view show";
            backtoShowMenuItem.Click += backtoShowMenuItem_Click;
            appBar.MenuItems.Add(backtoShowMenuItem);
           
        }

        void backtoShowMenuItem_Click(object sender, EventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + App.EpisodeViewModel.Tvdb, UriKind.Relative));
        }

        #endregion

        #region Shout Appbar

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

        private void ShoutsIconButton_Click(object sender, EventArgs e)
        {
            App.EpisodeViewModel.LoadShoutData(App.EpisodeViewModel.Tvdb, App.EpisodeViewModel.Season, App.EpisodeViewModel.Number);
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

                auth.Tvdb = App.EpisodeViewModel.Tvdb;
                auth.Title = App.EpisodeViewModel.ShowName;
                auth.Year = App.EpisodeViewModel.ShowYear;
                auth.Season = Int16.Parse(App.EpisodeViewModel.Season);
                auth.episode = Int16.Parse(App.EpisodeViewModel.Number);
                auth.Shout = (ShoutText.Text);
                watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/shout/episode/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
            }
        }

        void client_UploadShoutStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Episode", "Shout posted.");

                ShoutText.Text = "";

                this.Focus();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        #endregion

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                this.EpisodePanorama.Margin = new Thickness(0, 0, 0, 0);
                ShoutList.Width = 405;
                ShoutList.Height = 420;
            }
            else
            {
                this.EpisodePanorama.Margin = new Thickness(0, -180, 0, 0);
                ShoutList.Width = 800;
                ShoutList.Height = 340;
            }

            EpisodePanorama.DefaultItem = EpisodePanorama.Items[0];
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
            App.EpisodeViewModel = null;
        }
    }
}
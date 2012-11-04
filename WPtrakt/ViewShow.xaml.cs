using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;
using WPtrakt.Model.Trakt.Request;
using WPtrakt.Model.Trakt;
using System.Reflection;
using WPtrakt.Model;

namespace WPtrakt
{
    public partial class ViewShow : PhoneApplicationPage
    {
        public ViewShow()
        {
            InitializeComponent();

            DataContext = App.ShowViewModel;
            this.Loaded += new RoutedEventHandler(ViewShow_Loaded);
        }

        private void ViewShow_Loaded(object sender, RoutedEventArgs e)
        {
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            if (!String.IsNullOrEmpty(App.ShowViewModel.Tvdb))
            {
                RefreshBottomBar();
            }

            App.ShowViewModel.LoadData(id);  
        }

        private void MoviePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshBottomBar();
        }

        private void RefreshBottomBar()
        {
            if (this.ShowPanorama.SelectedIndex == 0)
            {
                InitAppBarMain();
            }
            else if (this.ShowPanorama.SelectedIndex == 1)
            {
                if (App.ShowViewModel.EpisodeItems.Count == 0)
                {
                    this.ApplicationBar.IsVisible = true;

                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    App.ShowViewModel.LoadEpisodeData(id);
                }
                InitAppBarSeasons();

            }
            else if (this.ShowPanorama.SelectedIndex == 2)
            {
                if (!App.ShowViewModel.ShoutsLoaded)
                {
                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    App.ShowViewModel.LoadShoutData(id);
                }
                InitAppBarShouts();

            }
        }

        #region Taps


        private void ImdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://www.imdb.com/title/" + App.ShowViewModel.Imdb);

            task.Show();
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (lastModel == null)
            {
                ListItemViewModel model = (ListItemViewModel)((StackPanel)sender).DataContext;
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
            }
        }

        private void SeasonPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel seasonPanel = (StackPanel)sender;

            short season = Int16.Parse((String)seasonPanel.DataContext);

            App.ShowViewModel.currentSeason = season;

            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.ShowViewModel.LoadEpisodeData(id);



            EpisodeGrid.Visibility = System.Windows.Visibility.Visible;
            SeasonGrid.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void TvdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://thetvdb.com/?tab=series&id=" + App.ShowViewModel.Tvdb);

            task.Show();
        }

        #endregion

        #region Main Appbar

        private Boolean ChangedSizeAlready = false;
        private void ListBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!ChangedSizeAlready)
            {
                InitAppBarMain();
                ChangedSizeAlready = true;
            }
        }

        private void InitAppBarMain()
        {

            ApplicationBar appBar = new ApplicationBar();

            if (!App.ShowViewModel.InWatchlist)
                CreateAddToWatchlist(appBar);
            else
                CreateRemoveFromWatchlist(appBar); 
            
            CreateRating(appBar);

            CreateWatchedButton(appBar, !App.ShowViewModel.Watched);

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
            if (StorageController.doesFileExist(App.ShowViewModel.Tvdb + "largebackground.jpg"))
            {
                ImageController.copyImageToShellContent(App.ShowViewModel.Tvdb + "largebackground.jpg", App.ShowViewModel.Tvdb);
                StandardTileData NewTileData = new StandardTileData
                {
                    BackgroundImage =
                         new Uri("isostore:/Shared/ShellContent/wptraktbg" + App.ShowViewModel.Tvdb + ".jpg", UriKind.Absolute),
                    BackContent = App.ShowViewModel.Name,
                };

                if (!StorageController.doesFileExist("/Shared/ShellContent/wptraktbg" + App.ShowViewModel.Tvdb + ".jpg"))
                {
                    NewTileData.BackgroundImage = new Uri("appdata:background.png"); ;
                }

                ShellTile.Create(
                new Uri(
                    "/ViewShow.xaml?id=" + App.ShowViewModel.Tvdb,
                    UriKind.Relative),
                    NewTileData);
            }

        }

        private void CreateAddToWatchlist(ApplicationBar appBar)
        {
            ApplicationBarIconButton AddtoWatchlist = new ApplicationBarIconButton();
            AddtoWatchlist = new ApplicationBarIconButton(new Uri("Images/appbar.feature.video.rest.png", UriKind.Relative));
            AddtoWatchlist.Click += new EventHandler(disabledAddtoWatchlist_Click);

            AddtoWatchlist.Text = "Watchlist +";
            appBar.Buttons.Add(AddtoWatchlist);
        }

        void disabledAddtoWatchlist_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadWatchlistStringCompleted);
            WatchlistAuth auth = new WatchlistAuth();
            auth.Shows = new TraktShow[1];
            auth.Shows[0] = new TraktShow();
            auth.Shows[0].imdb_id = App.ShowViewModel.Imdb;
            auth.Shows[0].Title = App.ShowViewModel.Name;
            auth.Shows[0].year = Int16.Parse(App.ShowViewModel.Year);
            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Show added to watchlist.");
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktShow.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + ".json");
                App.ShowViewModel.InWatchlist = true;
                InitAppBarMain();
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

            WatchlistAuth auth = new WatchlistAuth();
            auth.Shows = new TraktShow[1];
            auth.Shows[0] = new TraktShow();
            auth.Shows[0].imdb_id = App.ShowViewModel.Imdb;
            auth.Shows[0].Title = App.ShowViewModel.Name;
            auth.Shows[0].year = Int16.Parse(App.ShowViewModel.Year);

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadRemoveFromWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Show removed from watchlist.");
                App.ShowViewModel.InWatchlist = false;
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktShow.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + ".json");
                InitAppBarMain();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }


        private void CreateRating(ApplicationBar appBar)
        {
            ApplicationBarIconButton ratingButton = new ApplicationBarIconButton();
            ratingButton = new ApplicationBarIconButton(new Uri("Images/appbar.favs.rest.png", UriKind.Relative));
            ratingButton.IsEnabled = true;
            ratingButton.Text = "Rate";
            ratingButton.Click += new EventHandler(ratingButton_Click);

            appBar.Buttons.Add(ratingButton);
        }

        private void CreateWatchedButton(ApplicationBar appBar, Boolean enabled)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.IsEnabled = enabled;
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(WatchedIconButton_Click);

            appBar.Buttons.Add(watchedButton);
        }

        private void WatchedIconButton_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadSeenStringCompleted);
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
           
           
            auth.Imdb = App.ShowViewModel.Imdb;
            auth.Title = App.ShowViewModel.Name;
            auth.Year = Int16.Parse(App.ShowViewModel.Year);

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));
        }

        void client_UploadSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Show marked as watched.");
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktShow.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + ".json");
                App.ShowViewModel.Watched = true;
                InitAppBarMain();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        void ratingButton_Click(object sender, EventArgs e)
        {
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktShow.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + ".json");

            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=show&imdb=" + App.ShowViewModel.Imdb + "&year=" + App.ShowViewModel.Year + "&title=" + App.ShowViewModel.Name, UriKind.Relative));
        }

        #endregion

        #region Seasons Appbar

        private void InitAppBarSeasons()
        {
            ApplicationBar appBar = new ApplicationBar();

            ApplicationBarIconButton previousSeason = new ApplicationBarIconButton(new Uri("Images/appbar.back.rest.png", UriKind.Relative));
            previousSeason.Click += new EventHandler(ApplicationBarIconButton_Click_EpisodeBack);
            previousSeason.Text = "Previous";

            appBar.Buttons.Add(previousSeason);

            CreateSeasonsWatchedButton(appBar);

            ApplicationBarIconButton showSeasons = new ApplicationBarIconButton(new Uri("Images/appbar.phone.numbersign.rest.png", UriKind.Relative));
            showSeasons.Click += new EventHandler(showSeasons_Click);
            showSeasons.Text = "Seasons";


            appBar.Buttons.Add(showSeasons);
            CreateRefreshEpisodesButton(appBar);

            ApplicationBarIconButton nextSeason = new ApplicationBarIconButton(new Uri("Images/appbar.next.rest.png", UriKind.Relative));
            nextSeason.Click += new EventHandler(ApplicationBarIconButton_Click_EpisodeForward);

            nextSeason.Text = "Next";
            appBar.Buttons.Add(nextSeason);
            this.ApplicationBar = appBar;
        }

        private void CreateSeasonsWatchedButton(ApplicationBar appBar)
        {
            ApplicationBarMenuItem watchedMenuItem = new ApplicationBarMenuItem();
            watchedMenuItem.Text = "Mark season as seen.";
            watchedMenuItem.Click += new EventHandler(SeasonWatchedIconButton_Click);

            appBar.MenuItems.Add(watchedMenuItem);
        }

        private void SeasonWatchedIconButton_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadSeasonSeenStringCompleted);
            WatchedSeasonAuth auth = new WatchedSeasonAuth();

            auth.Imdb = App.ShowViewModel.Imdb;
            auth.Title = App.ShowViewModel.Name;
            auth.Year = Int16.Parse(App.ShowViewModel.Year);
            auth.Season = App.ShowViewModel.currentSeason;

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/season/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedSeasonAuth), auth));
        }

        void client_UploadSeasonSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Season marked as watched.");
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktEpisode.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + App.ShowViewModel.currentSeason + ".json");
                String id;
                NavigationContext.QueryString.TryGetValue("id", out id);
                App.ShowViewModel.LoadEpisodeData(id);
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void CreateRefreshEpisodesButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton refreshButton = new ApplicationBarIconButton();
            refreshButton = new ApplicationBarIconButton(new Uri("Images/appbar.refresh.rest.png", UriKind.Relative));
            refreshButton.Text = "Refresh";
            refreshButton.Click += new EventHandler(refreshButton_Click);

            appBar.Buttons.Add(refreshButton);
        }

        void refreshButton_Click(object sender, EventArgs e)
        {
            ReloadSeason();
        }

        void showSeasons_Click(object sender, EventArgs e)
        {
            EpisodeGrid.Visibility = System.Windows.Visibility.Collapsed;
            SeasonGrid.Visibility = System.Windows.Visibility.Visible;
        }


        private void PanoramaItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.ShowViewModel.LoadEpisodeData(id);
        }


        private void ApplicationBarIconButton_Click_EpisodeBack(object sender, EventArgs e)
        {
            if (App.ShowViewModel.currentSeason == 1)
                App.ShowViewModel.currentSeason = App.ShowViewModel.numberOfSeasons;
            else
                App.ShowViewModel.currentSeason -= 1;

            String id;
            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.ShowViewModel.LoadEpisodeData(id);
            InitAppBarSeasons();
        }

        private void ApplicationBarIconButton_Click_EpisodeForward(object sender, EventArgs e)
        {
            if (App.ShowViewModel.currentSeason == App.ShowViewModel.numberOfSeasons)
                App.ShowViewModel.currentSeason = 1;
            else
                App.ShowViewModel.currentSeason += 1;

            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.ShowViewModel.LoadEpisodeData(id);

            InitAppBarSeasons();
        }

        #endregion

        #region Shouts Appbar

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

                auth.Tvdb = App.ShowViewModel.Tvdb;
                auth.Title = App.ShowViewModel.Name;
                auth.Year = Int16.Parse(App.ShowViewModel.Year);

                auth.Shout = ShoutText.Text;
                LastShout = auth.Shout;
                watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/shout/show/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
            }
        }

        private void ShoutsIconButton_Click(object sender, EventArgs e)
        {
            App.ShowViewModel.LoadShoutData(App.ShowViewModel.Tvdb);
        }

        private String LastShout { get; set; }
        void client_UploadShoutStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Shout posted. It might take up to a minute for the shout to show up in the application.");

                ShoutText.Text = "";

                this.Focus();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        #endregion

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                this.ShowPanorama.Margin = new Thickness(0, 0, 0, 0);
                ShoutList.Width = 405;
                ShoutList.Height = 420;
                EpisodeList.Height = 440;
                SeasonsList.Width = 400;
                
            }
            else
            {
                this.ShowPanorama.Margin = new Thickness(0, -180, 0, 0);
                ShoutList.Width = 800;
                ShoutList.Height = 340;
                EpisodeList.Height = 360;
                SeasonsList.Width = 700;
            }

            ShowPanorama.DefaultItem = ShowPanorama.Items[0];
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.ShowViewModel = null;
            Animation.FadeOut(LayoutRoot);
        }

        #region EpisodeContextMenu

        private ListItemViewModel lastModel;

        private void SeenEpisode_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;
            var seenClient = new WebClient();

            seenClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadEpisodeSeenStringCompleted);
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = lastModel.Season;
            auth.Episodes[0].Episode = lastModel.Episode;
            auth.Imdb = App.ShowViewModel.Imdb;
            auth.Title = App.ShowViewModel.Name;
            auth.Year = Int16.Parse(App.ShowViewModel.Year);

            seenClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));
        }

        private void client_UploadEpisodeSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                lastModel.Watched = true;
                MessageBox.Show("Episode marked as watched.");
                
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null; 
        }

        private static void ReloadSeason()
        {
            StorageController.DeleteFile(TraktEpisode.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + App.ShowViewModel.currentSeason + ".json");
            App.ShowViewModel.LoadEpisodeData(App.ShowViewModel.Tvdb);
        }

        private void WatchlistEpisode_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            var watchlistClient = new WebClient();
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadEpisodeWatchlistStringCompleted);
           
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = lastModel.Season;
            auth.Episodes[0].Episode = lastModel.Episode;
            auth.Imdb = App.ShowViewModel.Imdb;
            auth.Title = App.ShowViewModel.Name;
            auth.Year = Int16.Parse(App.ShowViewModel.Year);

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

        }

        void client_UploadEpisodeWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                lastModel.InWatchList = true;
                MessageBox.Show("Episode added to watchlist.");
                
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null;
        }

        private void CheckinEpisode_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            var checkinClient = new WebClient();
            checkinClient.UploadStringCompleted += new UploadStringCompletedEventHandler(checkinClient_UploadStringCompleted);
            CheckinAuth auth = new CheckinAuth();

            auth.tvdb_id = App.ShowViewModel.Tvdb;
            auth.Title = App.ShowViewModel.Name;
            auth.year = Int16.Parse(App.ShowViewModel.Year);
            auth.Season = Int16.Parse(lastModel.Season);
            auth.Episode = Int16.Parse(lastModel.Episode);
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
                MessageBox.Show("Checked in!");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null;
        }

        #endregion



    }
}
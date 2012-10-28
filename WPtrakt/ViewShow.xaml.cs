using System;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using VPtrakt.Controllers;
using VPtrakt.Model.Trakt.Request;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Controllers;

namespace WPtrakt
{
    public partial class ViewShow : PhoneApplicationPage
    {
        private ApplicationBarIconButton previousSeason;
        private ApplicationBarIconButton nextSeason;

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
            if (!String.IsNullOrEmpty(App.ShowViewModel.GenreString))
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

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.ShowViewModel = null;
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandler = delegate { };
            completedHandler = delegate
            {
                storyboard.Completed -= completedHandler;
                storyboard.Stop();
            };
            storyboard.Completed += completedHandler;
            storyboard.Begin();
        }

        private void ImdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://www.imdb.com/title/" + App.ShowViewModel.Imdb);

            task.Show();
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };
            completedHandlerMainPage = delegate
            {

                StackPanel senderPanel = (StackPanel)sender;
                ListItemViewModel model = (ListItemViewModel)senderPanel.DataContext;
                NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };
            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
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
            if(StorageController.doesFileExist(App.ShowViewModel.Tvdb + "background.jpg"))
            {
            ImageController.copyImageToShellContent(App.ShowViewModel.Tvdb + "background.jpg", App.ShowViewModel.Tvdb);
            StandardTileData NewTileData = new StandardTileData
            {
                BackgroundImage =
                     new Uri("isostore:/Shared/ShellContent/wptraktbg" + App.ShowViewModel.Tvdb + ".jpg", UriKind.Absolute),
                BackContent = App.ShowViewModel.Name,
            };

            if (!StorageController.doesFileExist("/Shared/ShellContent/wptraktbg" + App.ShowViewModel.Tvdb + ".jpg"))
            {
                NewTileData.BackgroundImage = new Uri("appdata:background.png");;
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

            previousSeason = new ApplicationBarIconButton(new Uri("Images/appbar.back.rest.png", UriKind.Relative));
            previousSeason.Click += new EventHandler(ApplicationBarIconButton_Click_EpisodeBack);
            previousSeason.Text = "Previous";

            appBar.Buttons.Add(previousSeason);

            ApplicationBarIconButton showSeasons = new ApplicationBarIconButton(new Uri("Images/appbar.phone.numbersign.rest.png", UriKind.Relative));
            showSeasons.Click += new EventHandler(showSeasons_Click);
            showSeasons.Text = "Seasons";

            appBar.Buttons.Add(showSeasons);
            CreateRefreshEpisodesButton(appBar);

            nextSeason = new ApplicationBarIconButton(new Uri("Images/appbar.next.rest.png", UriKind.Relative));
            nextSeason.Click += new EventHandler(ApplicationBarIconButton_Click_EpisodeForward);

            nextSeason.Text = "Next";
            appBar.Buttons.Add(nextSeason);
            this.ApplicationBar = appBar;
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
            try
            {
                String fileNameEpisodes = TraktEpisode.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + App.ShowViewModel.currentSeason + ".json";

                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(fileNameEpisodes);
            }
            catch (IsolatedStorageException) { }
            App.ShowViewModel.LoadEpisodeData(App.ShowViewModel.Tvdb);
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
            this.Opacity = 1;
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
    }
}
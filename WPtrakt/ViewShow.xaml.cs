using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Linq;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtraktBase.Controller;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public partial class ViewShow : PhoneApplicationPage
    {
        private ShowController showController;
        private TraktShow Show;

        public ViewShow()
        {
            InitializeComponent();

            DataContext = App.ShowViewModel;
            this.showController = new ShowController();
            this.Loaded += new RoutedEventHandler(ViewShow_Loaded);
        }


        private void ViewShow_Loaded(object sender, RoutedEventArgs e)
        {
            String tvdb;
            NavigationContext.QueryString.TryGetValue("id", out tvdb);

            if (!String.IsNullOrEmpty(App.ShowViewModel.Tvdb))
            {
                RefreshBottomBar();
            }
            LoadShow(tvdb);
        }

        #region Load Show 

        private void LoadShow(String tvdb)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = false;
            worker.WorkerSupportsCancellation = false;
            worker.DoWork += new DoWorkEventHandler(showworker_DoWork);

            worker.RunWorkerAsync(tvdb);

            App.ShowViewModel.LoadData(tvdb);  
        }

        private async void showworker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Show = await showController.getShowByTVDBID(e.Argument.ToString());

            if (this.Show != null)
            {
                this.Show.Genres = this.Show.GenresAsString.Split('|');
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    App.ShowViewModel.UpdateShowView(this.Show);
                }));
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            if (this.Show.Seasons.Count == 0)
            {
                LoadSeasons(e.Argument.ToString());
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    App.ShowViewModel.NumberOfSeasons = (Int16)this.Show.Seasons.Count;

                }));
            }

            LoadBackgroundImage();
        }

        #endregion

        #region Load Seasons

        private async void LoadSeasons(String TvdbId)
        {
            TraktSeason[] seasons = await this.showController.getSeasonsByTVDBID(TvdbId);
            foreach (TraktSeason season in seasons)
                season.SeasonEpisodes = new EntitySet<TraktEpisode>();

            this.showController.AddSeasonsToShow(this.Show, seasons);
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                App.ShowViewModel.NumberOfSeasons = (Int16)this.Show.Seasons.Count;
            }));
        }

        #endregion

        #region Load Episodes

        public void LoadEpisodeData()
        {
            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            App.ShowViewModel.RefreshEpisodes();
            if (this.Show.Seasons.Count > 0)
            {
                if (App.ShowViewModel.currentSeason <= this.Show.Seasons.Count)
                {
                        BackgroundWorker episodesWorker = new BackgroundWorker();
                        episodesWorker.WorkerReportsProgress = false;
                        episodesWorker.WorkerSupportsCancellation = false;
                        episodesWorker.DoWork += new DoWorkEventHandler(episodesWorker_DoWork);

                        episodesWorker.RunWorkerAsync();
                }
            }
        }

        private async void episodesWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TraktEpisode[] episodes = await this.showController.getEpisodesOfSeason(this.Show, App.ShowViewModel.currentSeason);

            foreach (TraktEpisode episodeIt in episodes)
            {
                episodeIt.Tvdb = this.Show.tvdb_id;
                TraktEpisode episode = episodeIt;
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.ShowViewModel.EpisodeItems.Add(new ListItemViewModel() { Name = episode.Title, ImageSource = episode.Images.Screen, Imdb = this.Show.imdb_id + episode.Season + episode.Number, SubItemText = "Season " + episode.Season + ", Episode " + episode.Number, Episode = episode.Number, Season = episode.Season, Tvdb = this.Show.tvdb_id, Watched = episode.Watched, Rating = episode.MyRatingAdvanced, InWatchList = episode.InWatchlist });
                });
            }

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                App.ShowViewModel.EpisodeItems.Add(new ListItemViewModel());
                App.ShowViewModel.RefreshEpisodes();
            });
        }

        #endregion

        #region Load Shouts

        public async void LoadShoutData(String imdbId)
        {
            App.ShowViewModel.clearShouts();
            App.ShowViewModel.addShout(new ListItemViewModel() { Name = "Loading..." });
            try
            {
               TraktShout[] shouts = await this.showController.getShoutsForShow(this.Show.imdb_id);
               App.ShowViewModel.clearShouts();
              
                foreach (TraktShout shout in shouts)
                    App.ShowViewModel.addShout(new ListItemViewModel() { Name = shout.User.Username, ImageSource = shout.User.Avatar, Imdb = this.Show.imdb_id, SubItemText = shout.Shout });

                if (App.ShowViewModel.ShoutItems.Count == 0)
                    App.ShowViewModel.addShout(new ListItemViewModel() { Name = "No shouts" });

                App.ShowViewModel.ShoutsLoaded = true; ;
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException)
            { ErrorManager.ShowConnectionErrorPopup(); }
        }

        #endregion

        #region Load Fanart

        private async void LoadBackgroundImage()
        {
            String fileName = this.Show.tvdb_id + "background" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    App.ShowViewModel.BackgroundImage = ImageController.getImageFromStorage(fileName);
                }));
            }
            else
            {
                try
                {
                    HttpWebRequest request;

                    request = (HttpWebRequest)WebRequest.Create(new Uri(this.Show.Images.Fanart));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;

                    System.Net.HttpStatusCode status = webResponse.StatusCode;
                    if (status == System.Net.HttpStatusCode.OK)
                    {
                        Stream str = webResponse.GetResponseStream();

                        Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            App.ShowViewModel.BackgroundImage = ImageController.saveImage(this.Show.tvdb_id + "background.jpg", str, 800, 100);
                        }));
                    }
                }
                catch (WebException) { }
                catch (TargetInvocationException)
                { }
            }
        }

        #endregion 

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
                    LoadEpisodeData();
                }
                InitAppBarSeasons();

            }
            else if (this.ShowPanorama.SelectedIndex == 2)
            {
                if (!App.ShowViewModel.ShoutsLoaded)
                {
                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    this.LoadShoutData(id);
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
            LoadEpisodeData();

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
            if (StorageController.doesFileExist(App.ShowViewModel.Tvdb + "background.jpg"))
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
                    NewTileData.BackgroundImage = new Uri("appdata:background.png"); ;
                }

                ShellTile.Create(
                new Uri(
                    "/ViewShow.xaml?id=" + App.ShowViewModel.Tvdb,
                    UriKind.Relative),
                    NewTileData);
            }

        }

        #region Watchlist

        private void CreateAddToWatchlist(ApplicationBar appBar)
        {
            ApplicationBarIconButton AddtoWatchlist = new ApplicationBarIconButton();
            AddtoWatchlist = new ApplicationBarIconButton(new Uri("Images/appbar.feature.video.rest.png", UriKind.Relative));
            AddtoWatchlist.Click += new EventHandler(disabledAddtoWatchlist_Click);

            AddtoWatchlist.Text = "Watchlist +";
            appBar.Buttons.Add(AddtoWatchlist);
        }

        private async void disabledAddtoWatchlist_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarLoading.Visibility = System.Windows.Visibility.Visible;
                await this.showController.addShowToWatchlist(this.Show.tvdb_id, this.Show.imdb_id, this.Show.Title, this.Show.year);
                App.ShowViewModel.InWatchlist = true;

                ToastNotification.ShowToast("Show", "Show added to watchlist.");

                InitAppBarMain();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

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

        private async void removeFromWatchlist_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarLoading.Visibility = System.Windows.Visibility.Visible;
                await this.showController.removeShowFromWatchlist(this.Show.tvdb_id, this.Show.imdb_id, this.Show.Title, this.Show.year);
                ToastNotification.ShowToast("Show", "Show removed from watchlist.");
                App.ShowViewModel.InWatchlist = false;
                InitAppBarMain();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        #endregion

        #region Rating

        private void CreateRating(ApplicationBar appBar)
        {
            ApplicationBarIconButton ratingButton = new ApplicationBarIconButton();
            ratingButton = new ApplicationBarIconButton(new Uri("Images/appbar.favs.rest.png", UriKind.Relative));
            ratingButton.IsEnabled = true;
            ratingButton.Text = "Rate";
            ratingButton.Click += new EventHandler(ratingButton_Click);

            appBar.Buttons.Add(ratingButton);
        }

        void ratingButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=show&imdb=" + App.ShowViewModel.Imdb + "&year=" + App.ShowViewModel.Year + "&title=" + App.ShowViewModel.Name, UriKind.Relative));
        }

        #endregion

        #region Watched

        private void CreateWatchedButton(ApplicationBar appBar, Boolean enabled)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.IsEnabled = enabled;
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(WatchedIconButton_Click);

            appBar.Buttons.Add(watchedButton);
        }

        private async void WatchedIconButton_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarLoading.Visibility = System.Windows.Visibility.Visible;
                await showController.markShowAsSeen(this.Show.imdb_id, this.Show.Title, this.Show.year);
                ToastNotification.ShowToast("Show", "Show marked as watched.");
                App.ShowViewModel.Watched = true;
                InitAppBarMain();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        #endregion

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

        private async void SeasonWatchedIconButton_Click(object sender, EventArgs e)
        {
            try
            {
                progressBarLoading.Visibility = System.Windows.Visibility.Visible;
                await this.showController.markShowSeasonAsSeen(this.Show, App.ShowViewModel.currentSeason);
                ToastNotification.ShowToast("Show", "Season marked as watched.");
                LoadEpisodeData();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

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
            LoadEpisodeData();
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
            LoadEpisodeData();
        }


        private void ApplicationBarIconButton_Click_EpisodeBack(object sender, EventArgs e)
        {
            if (App.ShowViewModel.currentSeason == 1)
                App.ShowViewModel.currentSeason = App.ShowViewModel.NumberOfSeasons;
            else
                App.ShowViewModel.currentSeason -= 1;

            String id;
            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            NavigationContext.QueryString.TryGetValue("id", out id);
            LoadEpisodeData();
            InitAppBarSeasons();
        }

        private void ApplicationBarIconButton_Click_EpisodeForward(object sender, EventArgs e)
        {
            if (App.ShowViewModel.currentSeason == App.ShowViewModel.NumberOfSeasons)
                App.ShowViewModel.currentSeason = 1;
            else
                App.ShowViewModel.currentSeason += 1;

            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            LoadEpisodeData();

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

        private async void sendButton_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty((ShoutText.Text)))
            {
               try
                {
                    await this.showController.addShoutToShow(ShoutText.Text, this.Show.imdb_id, this.Show.Title, this.Show.year);
                    ToastNotification.ShowToast("Show", "Shout posted.");
                    ShoutText.Text = "";

                    this.Focus();
                }
                catch (WebException)
                {
                    ErrorManager.ShowConnectionErrorPopup();
                }
                catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
            }
        }

        private void ShoutsIconButton_Click(object sender, EventArgs e)
        {
            this.LoadShoutData(App.ShowViewModel.Tvdb);
        }

        private String LastShout { get; set; }
        void client_UploadShoutStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Show", "Shout posted.");

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
                ToastNotification.ShowToast("Show", "Episode marked as watched.");
                
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
            lastModel = null; 
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
                ToastNotification.ShowToast("Show", "Episode added to watchlist.");
                
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

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
                ToastNotification.ShowToast("Show", "Checked in!");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

            lastModel = null;
        }

        #endregion
    }
}
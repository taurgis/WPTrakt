using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtrakt.ViewModels;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public partial class Main : PhoneApplicationPage
    {
        private ShowController showController;
        private EpisodeController episodeController;
        private UserController userController;


        public Main()
        {
            InitializeComponent();
            DataContext = App.ViewModel;
            App.ViewModel.SetMainPage(this);
            this.showController = new ShowController();
            this.episodeController = new EpisodeController();
            this.userController = new UserController();
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            App.MainPage = this;
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
                    if ((String.IsNullOrEmpty(AppUser.Instance.UserName) || String.IsNullOrEmpty(AppUser.Instance.Password)))
                    {
                        AppUser.Instance.AppVersion = fullVersionNumber;
                        NavigationService.Navigate(new Uri("/Login.xaml", UriKind.Relative));
                    }
                    else
                    {
                        if (AppUser.Instance.AppVersion != fullVersionNumber)
                        {
                            MessageBox.Show("Application update. Clearing cache, the application will hang for a few seconds.");
                            AppUser.ClearCache();
                            AppUser.Instance.AppVersion = fullVersionNumber;
                        }

                        CallValidationService();
                    }
                }
            }
            catch (InvalidOperationException) { }
        }

        private DateTime firstCall { get; set; }
        DispatcherTimer userValidationTimer;

        private void CallValidationService()
        {
            firstCall = DateTime.Now;
            userValidationTimer = new DispatcherTimer();
            userValidationTimer.Interval = TimeSpan.FromSeconds(2);
            userValidationTimer.Tick += OnTimerTick;
            userValidationTimer.Start();

            HttpWebRequest request;

            request = (HttpWebRequest)WebRequest.Create(new Uri("https://api.trakt.tv/account/test/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
            request.Method = "POST";
            request.BeginGetRequestStream(new AsyncCallback(GetValidationRequestStreamCallback), request);
        }


        void GetValidationRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadValidationStringCompleted), webRequest);
        }

        void client_DownloadValidationStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                HttpStatusCode status = httpResoponse.StatusCode;

                if (status == System.Net.HttpStatusCode.OK)
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        userValidationTimer.Stop();

                        App.ViewModel.LoadData();
                    });
                }
            }
            catch (WebException)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    userValidationTimer.Stop();

                    App.ViewModel.Profile = new TraktProfileWithWatching();
                    App.ViewModel.NotifyPropertyChanged("LoadingStatus");
                    NavigationService.Navigate(new Uri("/Login.xaml", UriKind.Relative));
                    ToastNotification.ShowToast("User incorrect!", "Login data incorrect, or server connection problems.");

                });
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

        }

        void OnTimerTick(object sender, EventArgs e)
        {
            int seconds = (DateTime.Now - firstCall).Seconds;

            if (seconds > 10)
            {
                ToastNotification.ShowToast("Connection", "Slow connection to trakt!");
                userValidationTimer.Stop();
            }
        }

        public async void FadeInMainMenu()
        {
            

            App.ViewModel.clearWatching();

            if (App.ViewModel.Profile.GetType() == typeof(TraktProfileWithWatching))
            {
                if (((TraktProfileWithWatching)App.ViewModel.Profile).Watching != null)
                {
                    TraktLastActivity lastActivity = await userController.getLastActivityForUser();
                    if (((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Movie != null)
                    {
                        BitmapImage image = await showController.getFanartImage(((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Movie.imdb_id, ((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Movie.Images.Fanart);
                        this.LayoutRoot.Background = new ImageBrush
                        {
                            ImageSource = image,
                            Opacity = 0.0,
                            Stretch = Stretch.UniformToFill,
                        };
                        Animation.ImageFadeIn(this.LayoutRoot.Background);

                    }
                    else
                    {

                        TraktEpisode episode = ((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Episode;
                        TraktShow show = ((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Show;
                        Int64 lastCheckinScrobble = (lastActivity.Episode.Checkin > lastActivity.Episode.Scrobble) ? lastActivity.Episode.Checkin : lastActivity.Episode.Scrobble;

                        DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                        DateTime watchTime = baseTime.AddSeconds(lastCheckinScrobble);
                        ShowWatchingNow(episode, show, watchTime);
                    }
                }
                else
                {
                    if (this.LayoutRoot.Background != null)
                        this.LayoutRoot.Background = null;
                    this.WatchingNow.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                this.WatchingNow.Visibility = System.Windows.Visibility.Collapsed;
                if (this.LayoutRoot.Background != null)
                    this.LayoutRoot.Background = null;
            }

            LoadUpNextEpisodes();
        }

        public async void ShowWatchingNow(TraktEpisode episode, TraktShow show, DateTime watchTime)
        {
            this.WatchingNow.Visibility = System.Windows.Visibility.Visible;
            BitmapImage bitmapImage =  await showController.getFanartImage(show.tvdb_id, show.Images.Fanart);
            this.LayoutRoot.Background = new ImageBrush
                  {
                      ImageSource = bitmapImage,
                      Opacity = 0.0,
                      Stretch = Stretch.UniformToFill,
                  };

            App.ViewModel.clearWatching();
            this.WatchingNow.Visibility = System.Windows.Visibility.Visible;
            ListItemViewModel model = new ListItemViewModel() { Name = episode.Title, ImageSource = episode.Images.Screen, Imdb = episode.Tvdb + episode.Season + episode.Number, SubItemText = "Season " + episode.Season + ", Episode " + episode.Number, Episode = episode.Number, Season = episode.Season, Tvdb = show.tvdb_id, Watched = episode.Watched, Rating = episode.MyRatingAdvanced, InWatchList = episode.InWatchlist };



            TimeSpan percentageCompleteTimeSpan = DateTime.UtcNow - watchTime;


            model.WatchedCompletion = ((Double)percentageCompleteTimeSpan.Minutes / (Double)(show.Runtime)) * 100;



            App.ViewModel.WatchingNow.Add(model);
            Animation.ImageFadeIn(this.LayoutRoot.Background);
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
              {
                  App.ViewModel.NotifyPropertyChanged("WatchingNow");
              }));
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

        private Int32 lastSelection;
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Filter != null)
            {
                if (this.lastSelection != this.Filter.SelectedIndex)
                {
                    this.lastSelection = this.Filter.SelectedIndex;
                    App.ViewModel.FilterHistory(this.Filter.SelectedIndex);
                }
            }
        }

        #region Upnext episodes

        public async void LoadUpNextEpisodes()
        {
            App.ViewModel.clearUpNextItems();

            List<String> ignoredShowsList = new List<string>();

            foreach (TraktWatched watched in App.ViewModel.Profile.Watched)
            {
                if (watched.Show != null && !ignoredShowsList.Contains(watched.Show.tvdb_id))
                {


                    TraktShow show = await showController.getShowByTVDBID(watched.Show.tvdb_id);
                    ignoredShowsList.Add(show.tvdb_id);
                    if (show.Seasons.Count == 0)
                    {
                        TraktSeason[] seasons = await this.showController.getSeasonsByTVDBID(show.tvdb_id);
                        if (seasons != null)
                        {
                            foreach (TraktSeason season in seasons)
                                season.SeasonEpisodes = new EntitySet<TraktEpisode>();

                            this.showController.AddSeasonsToShow(show, seasons);
                        }
                    }

                    TraktEpisode[] episodes = await showController.getEpisodesOfSeason(show, Int16.Parse(watched.Episode.Season));

                    TraktEpisode nextEpisode = null;

                    foreach (TraktEpisode seasonEpisode in episodes)
                    {
                        if (Int16.Parse(seasonEpisode.Number) == (Int16.Parse(watched.Episode.Number) + 1))
                        {
                            nextEpisode = seasonEpisode;
                            break;
                        }
                    }

                    if (nextEpisode == null && show.Seasons.Count > Int16.Parse(watched.Episode.Season))
                    {
                        episodes = await showController.getEpisodesOfSeason(show, Int16.Parse(watched.Episode.Season + 1));

                        foreach (TraktEpisode seasonEpisode in episodes)
                        {
                            if (Int16.Parse(seasonEpisode.Number) == (Int16.Parse(watched.Episode.Number) + 1))
                            {
                                nextEpisode = seasonEpisode;
                                break;
                            }
                        }

                    }

                    if (nextEpisode != null)
                    {

                        App.ViewModel.UpNextItems.Add(new ListItemViewModel() { Name = show.Title + " (" + "S0" + nextEpisode.Season + "E" + nextEpisode.Number + ")", ImageSource = nextEpisode.Images.Screen, Imdb = show.imdb_id, Year = show.year, Episode = nextEpisode.Number, Season = nextEpisode.Season, Tvdb = nextEpisode.Tvdb, Watched = nextEpisode.Watched, Rating = nextEpisode.MyRatingAdvanced, InWatchList = nextEpisode.InWatchlist });

                        if (App.ViewModel.UpNextItems.Count == 2)
                        { break; }
                    }


                }
            }


            if (App.ViewModel.UpNextItems.Count > 0)
            {
                UpNextStackPanel.Visibility = System.Windows.Visibility.Visible;
                App.ViewModel.NotifyPropertyChanged("UpNextItems");

            }

            Animation.ControlFadeIn(this.MainMenuStackpanel);
        }

        #endregion

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
                this.Filter.SelectedIndex = 0;
                App.ViewModel.LoadHistoryData();
            }
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
            if (model.Type != null)
            {
                if (model.Type.Equals("episode"))
                    redirectUri = new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative);
                else if (model.Type.Equals("movie"))
                    redirectUri = new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative);
                else
                    redirectUri = new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative);

                Animation.NavigateToFadeOut(this, LayoutRoot, redirectUri);
            }
        }

        private void UpNextEpisodeGrid_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Grid)sender).DataContext;

            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
        }

        private void StackPanel_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((StackPanel)sender).DataContext;

            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));

        }

        #endregion

        #region Menu

        private void MyShows_Click_1(object sender, EventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/MyShows.xaml", UriKind.Relative));
        }

        private void MyMovies_Click_1(object sender, EventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/MyMovies.xaml", UriKind.Relative));
        }

        private void CancelCheckin_Click(object sender, EventArgs e)
        {
            var cancelCheckinClient = new WebClient();

            cancelCheckinClient.UploadStringCompleted += new UploadStringCompletedEventHandler(cancelCheckinClient_UploadStringCompleted);
            cancelCheckinClient.UploadStringAsync(new Uri("https://api.trakt.tv/movie/cancelcheckin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication());
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
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
        }

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        #endregion

        #region EpisodeContextMenu


        private async void SeenEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.progressBar.Visibility = System.Windows.Visibility.Visible;
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await episodeController.markEpisodeAsSeen(lastModel.Tvdb, lastModel.Imdb, lastModel.Name, lastModel.Year, lastModel.Season, lastModel.Episode))
            {
                lastModel.Watched = true;
                ToastNotification.ShowToast("Show", "Episode marked as watched.");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null;
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private async void WatchlistEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.progressBar.Visibility = System.Windows.Visibility.Visible;
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await episodeController.addEpisodeToWatchlist(lastModel.Tvdb, lastModel.Imdb, lastModel.Name, lastModel.Year, lastModel.Season, lastModel.Episode))
            {
                lastModel.InWatchList = true;
                ToastNotification.ShowToast("Show", "Episode added to watchlist.");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            lastModel = null;
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
        }


        private async void RemoveWatchlistEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.progressBar.Visibility = System.Windows.Visibility.Visible;
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await episodeController.removeEpisodeFromWatchlist(lastModel.Tvdb, lastModel.Imdb, lastModel.Name, lastModel.Year, lastModel.Season, lastModel.Episode))
            {
                lastModel.InWatchList = false;
                ToastNotification.ShowToast("Show", "Episode removed from watchlist.");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            lastModel = null;
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private async void UnSeenEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.progressBar.Visibility = System.Windows.Visibility.Visible;
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await episodeController.unMarkEpisodeAsSeen(lastModel.Tvdb, lastModel.Imdb, lastModel.Name, lastModel.Year, lastModel.Season, lastModel.Episode))
            {
                lastModel.Watched = false;
                ToastNotification.ShowToast("Show", "Episode unmarked as watched.");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            lastModel = null;
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private async void CheckinEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.progressBar.Visibility = System.Windows.Visibility.Visible;
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await episodeController.checkinEpisode(lastModel.Tvdb, lastModel.Name, lastModel.Year, lastModel.Season, lastModel.Episode))
            {
                lastModel.Watched = true;
                TraktShow show = await showController.getShowByTVDBID(lastModel.Tvdb);

                ShowWatchingNow(await episodeController.getEpisodeByTvdbAndSeasonInfo(lastModel.Tvdb, lastModel.Season, lastModel.Episode, show), show, DateTime.UtcNow);

                ToastNotification.ShowToast("Show", "Checked in!");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            lastModel = null;
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
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
            auth.Movies[0].imdb_id =lastModel.Imdb;
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

        #region HistoryContextMenu

        private void HistoryRate_Click(object sender, RoutedEventArgs e)
        {
            ActivityListItemViewModel model = (ActivityListItemViewModel)((MenuItem)sender).DataContext;
            switch (model.Type)
            {
                case "movie":
                      StorageController.DeleteFile(TraktMovie.getFolderStatic() + "/" + model.Imdb + ".json");
                      NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=movie&imdb=" + model.Imdb + "&year=" + model.Year + "&title=" + model.Name, UriKind.Relative));
                    break;
                case "show":
                    StorageController.DeleteFile(TraktShow.getFolderStatic() + "/" + model.Tvdb + ".json");
                    NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=show&imdb=" + model.Imdb + "&year=" + model.Year + "&title=" + model.Name, UriKind.Relative));
                    break;
                case "episode":
                    StorageController.DeleteFile(TraktWatched.getFolderStatic() + "/" + model.Tvdb + model.Season + model.Episode + ".json");
                    NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=episode&imdb=" + model.Imdb + "&year=" + model.Year + "&title=" + model.Name + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                    break;
            }
        }

        #endregion

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                HistoryList.Height = 550;
                HistoryList.Width = 420;
                ListTrending.Width = 700;
            }
            else
            {
                HistoryList.Height = 260;
                HistoryList.Width = 700;
                ListTrending.Width = 1370;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void RateApp_Click_1(object sender, EventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();

            marketplaceReviewTask.Show();
        }

        private void LogoutMenuItem_Click_1(object sender, EventArgs e)
        {
            AppUser.Instance.UserName = "";
            AppUser.Instance.Password = "";
            NavigationService.Navigate(new Uri("/Login.xaml", UriKind.Relative));
        }


    }
}
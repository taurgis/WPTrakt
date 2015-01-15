using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WPtrakt.Controllers;
using WPtrakt.Custom;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.ViewModels;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Threading;
using WPtrakt.Views;

namespace WPtrakt
{
    public partial class Main : ViewWithHistory
    {
        private ShowController showController;
        private EpisodeController episodeController;
        private UserController userController;
        private MovieController movieController;

        private Boolean Loading;
        private ProgressIndicator indicator;

        public Main()
        {
            InitializeComponent();
            DataContext = App.ViewModel;

            App.MainPage = this;
            this.showController = new ShowController();
            this.episodeController = new EpisodeController();
            this.userController = new UserController();
            this.movieController = new MovieController();

            this.Loading = false;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
            if (App.ViewModel.Profile != null)
            {
                lastModel = null;
                StorageController.IsNetworkStateCached = false;
                return;
            }

            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    App.TrackEvent("MainPage", "No internet connection");
                    ToastNotification.ShowToast("Connection", "No connection available!");
                    return;
                }

                var assembly = Assembly.GetExecutingAssembly().FullName;
                var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];

                if ((String.IsNullOrEmpty(AppUser.Instance.UserName) || String.IsNullOrEmpty(AppUser.Instance.Password)))
                {
                    App.TrackEvent("MainPage", "New user");
                    AppUser.Instance.AppVersion = fullVersionNumber;

                    NavigationService.Navigate(new Uri("/Login.xaml", UriKind.Relative));
                }
                else
                {
                    if (AppUser.Instance.AppVersion != fullVersionNumber)
                    {
                        MessageBox.Show("Application update. Clearing cache, the application will hang for a few seconds.");
                        App.TrackEvent("MainPage", "Updating application");
                        
                        AppUser.ClearCache();
                        AppUser.Instance.AppVersion = fullVersionNumber;
                    }

                    if (!AppUser.Instance.SmallScreenshotsEnabled && !(AppUser.Instance.ImagesWithWIFI && StorageController.IsConnectedToWifi()))
                    {
                        this.TrendingPanoramaItem.Visibility = System.Windows.Visibility.Collapsed;
                    }

                    LoadProfile();

                    TileHelper.StartReloadLiveTile();
                }

            }
            catch (InvalidOperationException error) {
                GoogleAnalytics.EasyTracker.GetTracker().SendException(error.Message, false);
            }
        }

        #region Profile

        private DateTime firstCall { get; set; }
        DispatcherTimer userValidationTimer;

        private async void LoadProfile()
        {
            App.TrackEvent("MainPage", "Load profile");
               

            InitiateSlowServerCounter();
 
            try
            {
                if (await userController.ValidateUser())
                {
                    TraktProfile profile = await userController.GetUserProfile();

                    if (profile != null)
                    {
                        App.ViewModel.Profile = profile;
                        userController.CreateContactBindingsAsync();

                        LoadWatchingNow();
                          
                    }
                    else
                    {
                        ValidationFailed();
                    }
                }
                else
                {
                    ValidationFailed(); 
                }
            }
            catch (TaskCanceledException)
            {
                ValidationFailed();
            }

            userValidationTimer.Stop();
        }

        private void InitiateSlowServerCounter()
        {
            if (this.userValidationTimer != null)
            {
                this.userValidationTimer.Stop();
            }

            this.firstCall = DateTime.Now;
            this.userValidationTimer = new DispatcherTimer();
            this.userValidationTimer.Interval = TimeSpan.FromSeconds(2);
            this.userValidationTimer.Tick += OnTimerTick;
            this.userValidationTimer.Start();
        }

        void OnTimerTick(object sender, EventArgs e)
        {
            int seconds = (DateTime.Now - firstCall).Seconds;

            if (seconds > 10)
            {
                App.TrackEvent("MainPage", "Slow connection to trakt");
                ToastNotification.ShowToast("Connection", "Slow connection to trakt!");
                userValidationTimer.Stop();
            }
        }

        private void ValidationFailed()
        {
            App.ViewModel.Profile = null;
            this.indicator.IsVisible = false;
            App.TrackEvent("MainPage", "Failed login");
            NavigationService.Navigate(new Uri("/Login.xaml", UriKind.Relative));
            ToastNotification.ShowToast("User incorrect!", "Login data incorrect, or server connection problems.");
        }

        public async void LoadWatchingNow()
        {
            App.ViewModel.clearWatching();

            if (App.ViewModel.Profile.GetType() == typeof(TraktProfileWithWatching))
            {
                if (((TraktProfileWithWatching)App.ViewModel.Profile).Watching != null)
                {
                    TraktLastActivity lastActivity = await userController.getLastActivityForUser();

                    if (((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Movie != null)
                    {
                        TraktMovie movie = await movieController.getMovieByImdbId(((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Movie.imdb_id);
                        if (movie != null)
                        {
                            Int64 lastCheckinScrobble = (lastActivity.Movie.Checkin > lastActivity.Movie.Scrobble) ? lastActivity.Movie.Checkin : lastActivity.Movie.Scrobble;
                            DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                            DateTime watchTime = baseTime.AddSeconds(lastCheckinScrobble);
                            ShowWatchingNowMovie(movie, watchTime);
                        }
                    }
                    else
                    {
                        Int64 lastCheckinScrobble = (lastActivity.Episode.Checkin > lastActivity.Episode.Scrobble) ? lastActivity.Episode.Checkin : lastActivity.Episode.Scrobble;
                        DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                        DateTime watchTime = baseTime.AddSeconds(lastCheckinScrobble);
                        TraktEpisode episode = ((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Episode;
                        TraktShow show = ((TraktProfileWithWatching)App.ViewModel.Profile).Watching.Show;
                        if (episode != null && show != null)
                        {
                            ShowWatchingNowShow(episode, show, watchTime);
                        }
                    }
                }
                else
                {
                    if (this.LayoutRoot.Background != null)
                        this.LayoutRoot.Background = null;

                    this.WatchingNowGrid.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                ClearWatchingNow();
            }
        }

        private void ClearWatchingNow()
        {
            this.WatchingNowGrid.Visibility = System.Windows.Visibility.Collapsed;
            App.ViewModel.WatchingNow = null;
            if (this.LayoutRoot.Background != null)
                this.LayoutRoot.Background = null;
        }


        public async void ShowWatchingNowMovie(TraktMovie movie, DateTime watchTime)
        {
            this.WatchingNowGrid.Visibility = System.Windows.Visibility.Visible;

            if ((AppUser.Instance.BackgroundWallpapersEnabled || (AppUser.Instance.ImagesWithWIFI && StorageController.IsConnectedToWifi())))
            {
                this.LayoutRoot.Background = new ImageBrush
                {
                    ImageSource = await movieController.getFanartImage(movie.imdb_id, movie.Images.Fanart),
                    Opacity = 0.0,
                    Stretch = Stretch.UniformToFill,
                };
            }

            App.ViewModel.clearWatching();
            this.WatchingNowGrid.Visibility = System.Windows.Visibility.Visible;

            ListItemViewModel model = new ListItemViewModel() { Type = "movie", Year = movie.year, Name = movie.Title, ImageSource = movie.Images.Fanart, Imdb = movie.imdb_id, SubItemText = movie.year.ToString() };
            model.LoadScreenImage();
            TimeSpan percentageCompleteTimeSpan = DateTime.UtcNow - watchTime;

            model.WatchedCompletion = ((Double)percentageCompleteTimeSpan.TotalMinutes / (Double)(movie.Runtime)) * 100;

            if (Double.IsInfinity(model.WatchedCompletion))
                model.WatchedCompletion = 0;

            App.ViewModel.WatchingNow = model;

            if (this.LayoutRoot.Background != null)
            {
                Animation.ImageFadeIn(this.LayoutRoot.Background);
            }
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                App.ViewModel.NotifyPropertyChanged("WatchingNow");
            }));
        }

        public async void ShowWatchingNowShow(TraktEpisode episode, TraktShow show, DateTime watchTime)
        {
            this.WatchingNowGrid.Visibility = System.Windows.Visibility.Visible;
            this.LayoutRoot.Background = new ImageBrush
            {
                ImageSource = await showController.getFanartImage(show.tvdb_id, show.Images.Fanart),
                Opacity = 0.0,
                Stretch = Stretch.UniformToFill,
            };

            App.ViewModel.clearWatching();
            this.WatchingNowGrid.Visibility = System.Windows.Visibility.Visible;
            ListItemViewModel model = new ListItemViewModel() { Type = "episode", Year = show.year, Name = show.Title, ImageSource = episode.Images.Screen, Imdb = show.tvdb_id + episode.Season + episode.Number, SubItemText = "Season " + episode.Season + ", Episode " + episode.Number, Episode = episode.Number, Season = episode.Season, Tvdb = show.tvdb_id, Watched = episode.Watched, Rating = episode.MyRatingAdvanced, InWatchList = episode.InWatchlist };
            model.LoadScreenImage();
            TimeSpan percentageCompleteTimeSpan = DateTime.UtcNow - watchTime;


            model.WatchedCompletion = ((Double)percentageCompleteTimeSpan.TotalMinutes / (Double)(show.Runtime)) * 100;
            if (Double.IsInfinity(model.WatchedCompletion))
                model.WatchedCompletion = 0;


            App.ViewModel.WatchingNow = model;


            if (this.LayoutRoot.Background != null)
            {
                Animation.ImageFadeIn(this.LayoutRoot.Background);
            }

            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                App.ViewModel.NotifyPropertyChanged("WatchingNow");
            }));
        }

        #endregion

        #region Recent
        public void loadRecent()
        {
            if (!Loading)
            {
                this.EmptyRecent.Visibility = System.Windows.Visibility.Collapsed;
                this.indicator = App.ShowLoading(this);
                this.Loading = true;
                App.ViewModel.clearRecent();
                TraktShow[] shows = showController.getRecentShows();
                TraktMovie[] movies = movieController.getRecentMovies();

                List<Object> mergedList = new List<object>();
                mergedList.AddRange(shows);
                mergedList.AddRange(movies);
                
                mergedList.Sort((x, y) => DateTime.Compare(DateTime.Parse(y.ToString()), (DateTime.Parse(x.ToString()))));

                foreach (Object movieOrShow in mergedList  )
                {


                    if (App.ViewModel.RecentItems.Count == 12)
                        break;

                    if(typeof(TraktShow) == movieOrShow.GetType() )
                    {
                        TraktShow show = (TraktShow) movieOrShow;
                        ListItemViewModel newModel = new ListItemViewModel() { ImageSource = show.Images.Poster, Tvdb = show.tvdb_id, Type="show" };
                        App.ViewModel.RecentItems.Add(newModel);
                         newModel.LoadPosterImage(); 
                    }
                    else
                    {
                         TraktMovie movie = (TraktMovie) movieOrShow;
                        ListItemViewModel newModel = new ListItemViewModel() { ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, Type="movie" };
                        App.ViewModel.RecentItems.Add(newModel);
                         newModel.LoadPosterImage(); 
                    }
                    
                }

                if (App.ViewModel.RecentItems.Count == 0)
                    this.EmptyRecent.Visibility = System.Windows.Visibility.Visible;

                App.ViewModel.NotifyPropertyChanged("RecentItems");
           
                this.indicator.IsVisible = false;
                this.Loading = false;
            }
        }
        #endregion

        #region Trending

        public async void loadTrending()
        {
            if (!Loading)
            {
                this.indicator = App.ShowLoading(this);
                this.Loading = true;
                App.ViewModel.ClearTrending();
                Random rand = new Random();
                int count = 0;
                foreach (TraktMovie movie in await movieController.GetTrendingMovies())
                {
                    if (++count > 4)
                        break;

                    if (count == 1)
                    {
                        App.ViewModel.TrendingItems.Add(new TrendingViewModel() { ImageSource = movie.Images.Fanart, ImageWidth = 420, Imdb = movie.imdb_id });
                    }
                    else
                    {
                        App.ViewModel.TrendingItems.Add(new TrendingViewModel() { ImageSource = movie.Images.Poster.Replace(".2.jpg", "-138.2.jpg"), ImageWidth = 130, Imdb = movie.imdb_id });
                    }

                    App.ViewModel.NotifyPropertyChanged("TrendingItems");
                }

                MoreTrending.Visibility = System.Windows.Visibility.Visible;

                this.indicator.IsVisible = false;
                this.Loading = false;
            }
        }

        #endregion

        #region History

        private Dictionary<DateTime, List<ActivityListItemViewModel>> sortedOrderHistory;
        private List<TraktActivity> newsFeedActivity;

        public async void LoadHistoryData()
        {
            if (!this.Loading)
            {
                App.TrackEvent("MainPage", "Load news feed");
                this.Loading = true;

                this.indicator = App.ShowLoading(this);

                App.ViewModel.clearHistory();

                CreateHistoryList(await userController.getNewsFeed(), 0);
            }
        }

        private bool isFilter = false;
       

        private void CreateHistoryList(List<TraktActivity> newsFeedActivity, int startDay)
        {
            this.newsFeedActivity = newsFeedActivity;
            sortedOrderHistory = null;
            isFilter = false;
            newsFeedActivity.Sort(TraktActivity.ActivityComparison);

            foreach (TraktActivity activity in newsFeedActivity)
            {
                ActivityListItemViewModel tempModel = null;
                try
                {
                    tempModel = DetermineActivity(activity, tempModel);

                    SortHistoryByDate(activity, tempModel, ref sortedOrderHistory);
                }
                catch (NullReferenceException) { }


            }

            if (sortedOrderHistory != null)
            {
                int count = 0;
                int dayCount = 1;
                foreach (DateTime key in sortedOrderHistory.Keys)
                {
                    if (dayCount++ < startDay + 1)
                        continue;

                    if (count++ == 6)
                        break;

                    Boolean isFirst = true;

                    foreach (ActivityListItemViewModel model in sortedOrderHistory[key])
                    {
                        if (isFirst)
                        {
                            model.HasHeader = true;
                            isFirst = false;

                            if(count == 5)
                                model.IsAlmostLastDay = true;
                        }



                        App.ViewModel.HistoryItems.Add(model);
                    }
                }
            }

            if (newsFeedActivity.Count == 0)
                ToastNotification.ShowToast("User", "News feed is empty!");

            App.ViewModel.NotifyPropertyChanged("HistoryItems");

            this.Loading = false;
            this.indicator.IsVisible = false;
        }

        public void FilterHistory(int type, int startDay)
        {
            isFilter = true;
            App.TrackEvent("MainPage", "Filter newsfeed to " + type);

            if (newsFeedActivity == null)
                return;

            sortedOrderHistory = null;

            if(this.timesLoaded == 1)
                App.ViewModel.clearHistory();

            foreach (TraktActivity activity in this.newsFeedActivity)
            {
                ActivityListItemViewModel activityModel = DetermineActivityWithFilter(type, activity);

                if (activityModel != null)
                    SortHistoryByDate(activity, activityModel, ref sortedOrderHistory);
            }

            int count = 0;
            int dayCount = 1;

            if (sortedOrderHistory != null)
            {
                foreach (DateTime key in sortedOrderHistory.Keys)
                {
                    if (dayCount++ < startDay + 1)
                        continue;

                    if (count++ == 6)
                        break;

                    Boolean isFirst = true;

                    foreach (ActivityListItemViewModel model in sortedOrderHistory[key])
                    {
                        if (isFirst)
                        {
                            model.HasHeader = true;
                            isFirst = false;

                            if (count == 5)
                                model.IsAlmostLastDay = true;
                        }

                        App.ViewModel.HistoryItems.Add(model);
                    }
                }
            }


            App.ViewModel.NotifyPropertyChanged("HistoryItems");
        }

        private Int32 lastSelection;
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (this.Filter != null)
            {
                this.timesLoaded = 1;
                if (this.Filter.SelectedIndex == 3)
                {
                    this.Filter.SelectedIndex = this.lastSelection;
                    return;
                }

                if (this.lastSelection != this.Filter.SelectedIndex)
                {
                    this.lastSelection = this.Filter.SelectedIndex;
                    FilterHistory(this.Filter.SelectedIndex, 0);
                    UpdateFilterHeaderText();
                }
            }
        }

        private void UpdateFilterHeaderText()
        {
            this.AllText.Visibility = System.Windows.Visibility.Collapsed;
            this.MeText.Visibility = System.Windows.Visibility.Collapsed;
            this.FriendsText.Visibility = System.Windows.Visibility.Collapsed;
            this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
            this.RatingText.Visibility = System.Windows.Visibility.Collapsed;
            this.CheckinText.Visibility = System.Windows.Visibility.Collapsed;
            this.ScrobbleText.Visibility = System.Windows.Visibility.Collapsed;
            this.ShoutText.Visibility = System.Windows.Visibility.Collapsed;

            switch (this.Filter.SelectedIndex)
            {
                case 0:
                    this.AllText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 1:
                    this.FriendsText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 2:
                    this.MeText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 4:
                    this.WatchlistText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 5:
                    this.RatingText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 6:
                    this.CheckinText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 7:
                    this.ScrobbleText.Visibility = System.Windows.Visibility.Visible;
                    break;
                case 8:
                    this.ShoutText.Visibility = System.Windows.Visibility.Visible;
                    break;
            }
        }

        int timesLoaded = 1;
        private void HistoryList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            ActivityListItemViewModel model = (ActivityListItemViewModel)e.Container.DataContext;
            model.LoadScreenImage();

            if (model.IsAlmostLastDay && !model.hasBeenUnrealized)
            {
                if (!this.isFilter)
                    CreateHistoryList(this.newsFeedActivity, timesLoaded++ * 6);
                else
                    FilterHistory(this.lastSelection, timesLoaded++ * 6);
            }
        }

        private void HistoryList_ItemUnrealized(object sender, ItemRealizationEventArgs e)
        {
            ActivityListItemViewModel model = (ActivityListItemViewModel)e.Container.DataContext;
            model.ClearScreenImage();
            model.hasBeenUnrealized = true;
        }

        #endregion

        #region Taps

        private void AllText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.Filter.Open();
        }

        private void Search_Click(object sender, EventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/Search.xaml", UriKind.Relative));
        }

        private void TrendingImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (lastModel == null)
            {
                TrendingViewModel model = (TrendingViewModel)((Image)sender).DataContext;
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
            }
        }

        private void MoreTrending_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewTrending.xaml", UriKind.Relative));
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


        private void WatchingNowGrid_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Grid temp = (Grid)sender;

            ContextMenu contextMenu = ContextMenuService.GetContextMenu(temp);

            if (contextMenu.Parent == null)
            {
                contextMenu.IsOpen = true;
            }

        }

        private void GestureListener_Tap_1(object sender, GestureEventArgs e)
        {
            if (lastModel == null)
            {
                Grid temp = (Grid)sender;

                ContextMenu contextMenu = ContextMenuService.GetContextMenu(temp);

                if (contextMenu.Parent == null)
                {
                    contextMenu.IsOpen = true;
                }
            }
        }

        private void CheckinHistoryMenuItem_Click_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/CheckinHistory.xaml", UriKind.Relative));
        }

        private void UpcommingText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/Upcomming.xaml", UriKind.RelativeOrAbsolute));
        }

        private void MovieText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/MyMovies.xaml", UriKind.Relative));
        }

        private void ShowText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/MyShows.xaml", UriKind.Relative));
        }

        #endregion

        #region Menu

        private void RateApp_Click_1(object sender, EventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();

            marketplaceReviewTask.Show();
        }

        private void LogoutMenuItem_Click_1(object sender, EventArgs e)
        {
            AppUser.Instance.UserName = "";
            AppUser.Instance.Password = "";
            UserController.ClearContactData();
            App.ViewModel = null;
            NavigationService.Navigate(new Uri("/Login.xaml", UriKind.Relative));
        }

        private void FriendsMenuItem_Click_1(object sender, EventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/Friends.xaml", UriKind.Relative));
        }

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void ApplicationBarRefreshButton_Click(object sender, EventArgs e)
        {
            if (MainPanorama.SelectedIndex == 0)
            {
                App.ViewModel.Profile = null;

                LoadProfile();
            }
            else if(MainPanorama.SelectedIndex == 1)
            {
                loadRecent();
            }
            else if (MainPanorama.SelectedIndex == 2)
            {
                loadTrending();
            }
            else if (MainPanorama.SelectedIndex == 3)
            {
                this.Filter.SelectedIndex = 0;
                UpdateFilterHeaderText();
                LoadHistoryData();
            }
        }


        #endregion

        #region EpisodeContextMenu

        private void WatchingRate_Click(object sender, RoutedEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((MenuItem)sender).DataContext;
            lastModel = model;

            switch (model.Type)
            {
                case "movie":
                    NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=movie&imdb=" + model.Imdb + "&year=" + model.Year + "&title=" + model.Name, UriKind.Relative));
                    break;

                case "episode":
                    NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=episode&imdb=" + model.Imdb + "&tvdb=" + model.Tvdb + "&year=" + model.Year + "&title=" + model.Name + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                    break;
            }


        }

        private void RecentlyWatched_Click_1(object sender, RoutedEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((MenuItem)sender).DataContext;
            lastModel = model;

            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/CheckinHistory.xaml", UriKind.Relative));

        }



        private void EpisodeRate_Click(object sender, RoutedEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((MenuItem)sender).DataContext;
            lastModel = model;
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=episode&imdb=" + model.Imdb + "&tvdb=" + model.Tvdb + "&year=" + model.Year + "&title=" + model.Name + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));

        }

        private async void CancelCheckinEpisode_Click_1(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;
            this.indicator = App.ShowLoading(this);
            if (await userController.CancelActiveCheckin())
            {
                ClearWatchingNow();
                ToastNotification.ShowToast("Cancel", "Cancelled any active check in!");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            this.indicator.IsVisible = false;
            lastModel = null;
        }

        private async void SeenEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.indicator = App.ShowLoading(this);
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
            this.indicator.IsVisible = false;
        }

        private async void WatchlistEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.indicator = App.ShowLoading(this);
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
            this.indicator.IsVisible = false;
        }


        private async void RemoveWatchlistEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.indicator = App.ShowLoading(this);
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
            this.indicator.IsVisible = false;
        }

        private async void UnSeenEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.indicator = App.ShowLoading(this);
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
            this.indicator.IsVisible = false;
        }

        private async void CheckinEpisode_Click(object sender, RoutedEventArgs e)
        {
            this.indicator = App.ShowLoading(this);
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await episodeController.checkinEpisode(lastModel.Tvdb, lastModel.Name, lastModel.Year, lastModel.Season, lastModel.Episode))
            {
                if (lastModel != null)
                {
                    lastModel.Watched = true;
                }
                TraktShow show = await showController.getShowByTVDBID(lastModel.Tvdb);

                ShowWatchingNowShow(await episodeController.getEpisodeByTvdbAndSeasonInfo(lastModel.Tvdb, lastModel.Season, lastModel.Episode, show), show, DateTime.UtcNow);

                ToastNotification.ShowToast("Show", "Checked in!");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            lastModel = null;
            this.indicator.IsVisible = false;
        }

        private void ViewEpisode_Click_1(object sender, RoutedEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((MenuItem)sender).DataContext;
            lastModel = model;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));

        }

        private void ViewShow_Click_1(object sender, RoutedEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((MenuItem)sender).DataContext;
            lastModel = model;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));

        }

        #endregion

        #region MovieContextMenu

        private ListItemViewModel lastModel;

        private async void SeenMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await movieController.markMovieAsSeen(lastModel.Imdb, lastModel.Name, lastModel.Year))
            {
                lastModel.Watched = true;
                ToastNotification.ShowToast("Movie", "Movie marked as watched.");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            lastModel = null;
        }

        private async void WatchlistMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await movieController.addMovieToWatchlist(lastModel.Imdb, lastModel.Name, lastModel.Year))
            {
                lastModel.InWatchList = true;
                ToastNotification.ShowToast("Movie", "Movie added to watchlist.");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null;
        }

        private async void CheckinMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await movieController.checkinMovie(lastModel.Imdb, lastModel.Name, lastModel.Year))
            {
                ShowWatchingNowMovie(await movieController.getMovieByImdbId(lastModel.Imdb), DateTime.UtcNow);
                ToastNotification.ShowToast("Movie", "Checked in!");
            }
            else
            {
                ToastNotification.ShowToast("Movie", "There is already a checkin in progress.");
            }
        }

        #endregion

        #region HistoryContextMenu

        private void HistoryRate_Click(object sender, RoutedEventArgs e)
        {
            ActivityListItemViewModel model = (ActivityListItemViewModel)((MenuItem)sender).DataContext;
            switch (model.Type)
            {
                case "movie":
                    NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=movie&imdb=" + model.Imdb + "&year=" + model.Year + "&title=" + model.Name, UriKind.Relative));
                    break;
                case "show":
                    NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=show&imdb=" + model.Imdb + "&tvdb=" + model.Tvdb + "&year=" + model.Year + "&title=" + model.Name, UriKind.Relative));
                    break;
                case "episode":
                    NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=episode&imdb=" + model.Imdb + "&tvdb=" + model.Tvdb + "&year=" + model.Year + "&title=" + model.Name + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                    break;
            }
        }

        #endregion

        #region WatchingNowContextMenu

        private void ViewEpisodeNow_Click(object sender, RoutedEventArgs e)
        {

            ListItemViewModel model = (ListItemViewModel)((MenuItem)sender).DataContext;


            switch (model.Type)
            {
                case "episode":
                    NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                    break;

                case "movie":
                    NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
                    break;
            }
        }

        #endregion

        private Boolean wallpaperNavigationFinished = false;
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string lockscreenKey = "WallpaperSettings";
            string lockscreenValue = "0";

            bool lockscreenValueExists = NavigationContext.QueryString.TryGetValue(lockscreenKey, out lockscreenValue);

            if (!wallpaperNavigationFinished)
            {
                if (lockscreenValueExists)
                {
                    NavigationService.Navigate(new Uri("/Settings.xaml?lockscreen=true", UriKind.Relative));
                    wallpaperNavigationFinished = true;
                }

            }
        }

        private void MainPanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.TrackEvent("MainPage", "Switched panorama to " + MainPanorama.SelectedIndex);
            if(MainPanorama.SelectedIndex == 1)
            {
                loadRecent();
            }
            else if (MainPanorama.SelectedIndex == 2)
            {
                if (App.ViewModel.TrendingItems.Count == 0)
                {
                    loadTrending();
                }
            }
            else if (MainPanorama.SelectedIndex == 3)
            {
                if (App.ViewModel.HistoryItems.Count == 0)
                {
                    LoadHistoryData();
                }
            }
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Image)sender).DataContext;

            if(model.Type.Equals("show"))
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
            else
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }
    }
}
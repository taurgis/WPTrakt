using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WPtrakt.Controllers;
using WPtrakt.Custom;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
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
        private MovieController movieController;
        private Boolean Loading;

        public Main()
        {
            InitializeComponent();
            DataContext = App.ViewModel;

            this.Loading = false;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
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
                    ToastNotification.ShowToast("Connection", "No connection available!");
                    return;
                }


                App.ViewModel.SetMainPage(this);
                App.MainPage = this;
                this.showController = new ShowController();
                this.episodeController = new EpisodeController();
                this.userController = new UserController();
                this.movieController = new MovieController();

                var assembly = Assembly.GetExecutingAssembly().FullName;
                var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];

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

                    if (!AppUser.Instance.SmallScreenshotsEnabled && !(AppUser.Instance.ImagesWithWIFI && StorageController.IsConnectedToWifi()))
                    {
                        this.TrendingPanoramaItem.Visibility = System.Windows.Visibility.Collapsed;
                    }

                    LoadProfile();

                    ReloadLiveTile();
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

        #region Profile

        private DateTime firstCall { get; set; }
        DispatcherTimer userValidationTimer;

        private async void LoadProfile()
        {
            if (!this.Loading)
            {
                this.Loading = true;

                InitiateSlowServerCounter();

                this.progressBar.Visibility = System.Windows.Visibility.Visible;

                if (await userController.ValidateUser())
                {
                    userValidationTimer.Stop();

                    TraktProfile profile = await userController.GetUserProfile();

                    if (profile != null)
                    {
                        App.ViewModel.Profile = profile;
                        App.ViewModel.RefreshProfile();

                        FadeInHomeScreen();
                    }
                    else
                    {
                        ValidationFailed();
                        this.Loading = false;
                    }
                }
                else
                {
                    ValidationFailed();
                    userValidationTimer.Stop();
                    this.Loading = false;
                }
            }
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
                ToastNotification.ShowToast("Connection", "Slow connection to trakt!");
                userValidationTimer.Stop();
            }
        }

        private void ValidationFailed()
        {
            App.ViewModel.Profile = null;
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
            NavigationService.Navigate(new Uri("/Login.xaml", UriKind.Relative));
            ToastNotification.ShowToast("User incorrect!", "Login data incorrect, or server connection problems.");
        }

        public async void FadeInHomeScreen()
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

                    this.WatchingNow.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                ClearWatchingNow();
            }

            LoadUpNextEpisodes();
        }

        private void ClearWatchingNow()
        {
            this.WatchingNow.Visibility = System.Windows.Visibility.Collapsed;

            if (this.LayoutRoot.Background != null)
                this.LayoutRoot.Background = null;
        }


        public async void ShowWatchingNowMovie(TraktMovie movie, DateTime watchTime)
        {
            this.WatchingNow.Visibility = System.Windows.Visibility.Visible;

            BitmapImage image = await movieController.getFanartImage(movie.imdb_id, movie.Images.Fanart);
            this.LayoutRoot.Background = new ImageBrush
            {
                ImageSource = image,
                Opacity = 0.0,
                Stretch = Stretch.UniformToFill,
            };


            App.ViewModel.clearWatching();
            this.WatchingNow.Visibility = System.Windows.Visibility.Visible;

            ListItemViewModel model = new ListItemViewModel() { Type = "movie", Year = movie.year, Name = movie.Title, ImageSource = movie.Images.Fanart, Imdb = movie.imdb_id, SubItemText = movie.year.ToString() };

            TimeSpan percentageCompleteTimeSpan = DateTime.UtcNow - watchTime;

            model.WatchedCompletion = ((Double)percentageCompleteTimeSpan.TotalMinutes / (Double)(movie.Runtime)) * 100;

            if (Double.IsInfinity(model.WatchedCompletion))
                model.WatchedCompletion = 0;

            App.ViewModel.WatchingNow.Add(model);

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
            this.WatchingNow.Visibility = System.Windows.Visibility.Visible;

            BitmapImage image = await showController.getFanartImage(show.tvdb_id, show.Images.Fanart);
            this.LayoutRoot.Background = new ImageBrush
            {
                ImageSource = image,
                Opacity = 0.0,
                Stretch = Stretch.UniformToFill,
            };

            App.ViewModel.clearWatching();
            this.WatchingNow.Visibility = System.Windows.Visibility.Visible;
            ListItemViewModel model = new ListItemViewModel() { Type = "episode", Year = show.year, Name = show.Title, ImageSource = episode.Images.Screen, Imdb = show.tvdb_id + episode.Season + episode.Number, SubItemText = "Season " + episode.Season + ", Episode " + episode.Number, Episode = episode.Number, Season = episode.Season, Tvdb = show.tvdb_id, Watched = episode.Watched, Rating = episode.MyRatingAdvanced, InWatchList = episode.InWatchlist };

            TimeSpan percentageCompleteTimeSpan = DateTime.UtcNow - watchTime;


            model.WatchedCompletion = ((Double)percentageCompleteTimeSpan.TotalMinutes / (Double)(show.Runtime)) * 100;
            if (Double.IsInfinity(model.WatchedCompletion))
                model.WatchedCompletion = 0;


            App.ViewModel.WatchingNow.Add(model);

            if (this.LayoutRoot.Background != null)
            {
                Animation.ImageFadeIn(this.LayoutRoot.Background);
            }

            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                App.ViewModel.NotifyPropertyChanged("WatchingNow");
            }));
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
                        episodes = await showController.getEpisodesOfSeason(show, (Int16)(Int16.Parse(watched.Episode.Season) + 1));

                        foreach (TraktEpisode seasonEpisode in episodes)
                        {
                            if (Int16.Parse(seasonEpisode.Number) == 1)
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

                if ((this.Orientation == PageOrientation.PortraitDown) || (this.Orientation == PageOrientation.PortraitUp))
                {
                    UpNextStackPanel.Visibility = System.Windows.Visibility.Visible;
                }

                App.ViewModel.NotifyPropertyChanged("UpNextItems");

            }
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
            this.MainPanorama.Visibility = System.Windows.Visibility.Visible;
            Loading = false;
            Animation.ControlFadeInSlow(this.MainPanorama);
        }

        #endregion

        #endregion

        #region Trending

        public async void loadTrending()
        {
            if (!Loading)
            {
                this.progressBar.Visibility = System.Windows.Visibility.Visible;
                this.Loading = true;
                App.ViewModel.ClearTrending();

                int count = 0;
                foreach (TraktMovie movie in await movieController.GetTrendingMovies())
                {
                    if (++count > 8)
                        break;


                    App.ViewModel.TrendingItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, Watched = movie.Watched, Rating = movie.MyRatingAdvanced, NumberOfRatings = movie.Ratings.Votes.ToString(), Type = "Movie", InWatchList = movie.InWatchlist, SubItemText = movie.year.ToString(), Year = movie.year });
                    App.ViewModel.NotifyPropertyChanged("TrendingItems");
                }

                this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
                this.Loading = false;
            }
        }

        #endregion

        #region History

        private List<TraktActivity> newsFeedActivity;

        public async void LoadHistoryData()
        {
            if (!this.Loading)
            {
                this.Loading = true;

                this.progressBar.Visibility = System.Windows.Visibility.Visible;

                App.ViewModel.clearHistory();

                CreateHistoryList(await userController.getNewsFeed());
            }
        }

        private Dictionary<DateTime, List<ActivityListItemViewModel>> sortedOrderHistory;

        private void CreateHistoryList(List<TraktActivity> newsFeedActivity)
        {
            int counter = 0;
            this.newsFeedActivity = newsFeedActivity;
            sortedOrderHistory = null;
            newsFeedActivity.Sort(TraktActivity.ActivityComparison);
            foreach (TraktActivity activity in newsFeedActivity)
            {
                ActivityListItemViewModel tempModel = null;
                try
                {
                    if (counter++ <= 40)
                    {
                        switch (activity.Action)
                        {
                            case "watchlist":
                                tempModel = AddToWatchList(activity);
                                break;
                            case "rating":
                                tempModel = Rated(activity);
                                break;
                            case "checkin":
                                tempModel = Checkin(activity);
                                break;
                            case "scrobble":
                                tempModel = Scrobble(activity);
                                break;
                            case "collection":
                                tempModel = Collection(activity);
                                break;
                            case "shout":
                                tempModel = Shout(activity);
                                break;
                        }

                        OrderHistory(activity, tempModel);
                    }


                }
                catch (NullReferenceException) { }


            }

            foreach (DateTime key in sortedOrderHistory.Keys)
            {
                Boolean isFirst = true;

                foreach (ActivityListItemViewModel model in sortedOrderHistory[key])
                {
                    if (isFirst)
                    {
                        model.HasHeader = true;
                        isFirst = false;
                    }

             
                    App.ViewModel.HistoryItems.Add(model);
                }

            }

            if (newsFeedActivity.Count == 0)
                ToastNotification.ShowToast("User", "News feed is empty!");

            App.ViewModel.NotifyPropertyChanged("HistoryItems");
            this.Loading = false;
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
        }

    
        private void OrderHistory(TraktActivity activity, ActivityListItemViewModel tempModel)
        {
            if (sortedOrderHistory == null)
            {
                sortedOrderHistory = new Dictionary<DateTime,List<ActivityListItemViewModel>>();
            }


              DateTime time = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
               time = time.AddSeconds(activity.TimeStamp);
               time = time.ToLocalTime();
             DateTime onlyDay = new DateTime(time.Year, time.Month, time.Day);

             tempModel.Date = onlyDay;

             if (sortedOrderHistory.ContainsKey(onlyDay))
             {
                
                 sortedOrderHistory[onlyDay].Add(tempModel);
             }
             else
             {
                 List<ActivityListItemViewModel> tempList = new List<ActivityListItemViewModel>();
                 tempList.Add(tempModel);
                 sortedOrderHistory.Add(onlyDay, tempList);
             }
        }

        public void FilterHistory(int type)
        {
            if (newsFeedActivity == null)
                return;

            int counter = 0;
            App.ViewModel.clearHistory();
            foreach (TraktActivity activity in this.newsFeedActivity)
            {
                ActivityListItemViewModel tempModel = null;

                try
                {
                    if (counter++ <= 40)
                    {
                        switch (activity.Action)
                        {
                            case "watchlist":
                                if (type == 1 || type == 0)
                                    tempModel = AddToWatchList(activity);
                                break;
                            case "rating":
                                if (type == 2 || type == 0)
                                    tempModel = Rated(activity);
                                break;
                            case "checkin":
                                if (type == 3 || type == 0)
                                    tempModel = Checkin(activity);
                                break;
                            case "scrobble":
                                if (type == 4 || type == 0)
                                    tempModel = Scrobble(activity);
                                break;
                            case "collection":
                                if (type == 5 || type == 0)
                                    tempModel = Collection(activity);
                                break;
                            case "shout":
                                if (type == 6 || type == 0)
                                    tempModel = Shout(activity);
                                break;
                        }

                        if (tempModel != null)
                            OrderHistory(activity, tempModel);
                    }
                }
                catch (NullReferenceException) { }
            }

            App.ViewModel.NotifyPropertyChanged("HistoryItems");
        }

        private ActivityListItemViewModel Shout(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to movie " + activity.Movie.Title + "" , Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to show " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to episode " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Collection(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Movie.Title + " to the collection" , Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Show.Title + " to the collection.", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + "to the collection", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Scrobble(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "scrobbled " + activity.Movie.Title + "" , Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "scrobbled " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "scrobbled " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Checkin(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "checked in " + activity.Movie.Title , Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "checked in " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "checked in " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Rated(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "rated " + activity.Movie.Title + ": " + activity.RatingAdvanced + "/10 " , Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "rated " + activity.Show.Title + ": " + activity.RatingAdvanced + "/10 .", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "rated " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ": " + activity.RatingAdvanced + "/10 .", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }
            return null;
        }

        private ActivityListItemViewModel AddToWatchList(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Movie.Title + " to the watchlist" , Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Show.Title + " to the watchlist.", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + "to the watchlist", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }


        #endregion

        #region Taps

        private void ApplicationBarRefreshButton_Click(object sender, EventArgs e)
        {
            if (MainPanorama.SelectedIndex == 0)
            {
                StorageController.DeleteFile(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json");
                this.MainPanorama.Visibility = System.Windows.Visibility.Collapsed;
                App.ViewModel.Profile = null;
                App.ViewModel.NotifyPropertyChanged("LoadingStatus");
                this.progressBar.Visibility = System.Windows.Visibility.Visible;
                this.MainPanorama.Opacity = 0;

                LoadProfile();
            }
            else if (MainPanorama.SelectedIndex == 1)
            {
                loadTrending();
            }
            else if (MainPanorama.SelectedIndex == 2)
            {
                this.Filter.SelectedIndex = 0;
                LoadHistoryData();
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
                ListItemViewModel model = (ListItemViewModel)((DelayLoadImage)sender).DataContext;
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

        private void StackPanel_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((StackPanel)sender).DataContext;

            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));

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

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
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


        private void EpisodeRate_Click(object sender, RoutedEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((MenuItem)sender).DataContext;
            lastModel = model;
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=episode&imdb=" + model.Imdb + "&tvdb=" + model.Tvdb + "&year=" + model.Year + "&title=" + model.Name + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));

        }

        private async void CancelCheckinEpisode_Click_1(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            if (await userController.CancelActiveCheckin())
            {
                ClearWatchingNow();
                ToastNotification.ShowToast("Cancel", "Cancelled any active check in!");
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }

            lastModel = null;
        }

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
            this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
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

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                AvatarPanel.Visibility = System.Windows.Visibility.Visible;
                if (App.ViewModel.UpNextItems.Count > 0)
                {
                    UpNextStackPanel.Visibility = System.Windows.Visibility.Visible;
                }
                HistoryList.Width = 420;
                ListTrending.Width = 700;
            }
            else
            {
                AvatarPanel.Visibility = System.Windows.Visibility.Collapsed;
                UpNextStackPanel.Visibility = System.Windows.Visibility.Collapsed;
                HistoryList.Width = 700;
                ListTrending.Width = 1370;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

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
            if (MainPanorama.SelectedIndex == 1)
            {
                if (App.ViewModel.TrendingItems.Count == 0)
                {
                    loadTrending();
                }
            }
            else if (MainPanorama.SelectedIndex == 2)
            {
                if (App.ViewModel.HistoryItems.Count == 0)
                {
                    LoadHistoryData();
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
                    FilterHistory(this.Filter.SelectedIndex);
                }
            }
        }
    }
}
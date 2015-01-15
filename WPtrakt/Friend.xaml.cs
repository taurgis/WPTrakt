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

namespace WPtrakt
{
    public partial class Friend : PhoneApplicationPage
    {
        private UserController userController;
        private Boolean Loading;
        private ProgressIndicator indicator;
        private String username;
        public Friend()
        {
            InitializeComponent();
            DataContext = App.FriendViewModel;

            this.Loading = false;
            this.Loaded += new RoutedEventHandler(Friends_Loaded);
        }

        private void Friends_Loaded(object sender, RoutedEventArgs e)
        {
            this.userController = new UserController();

            String isTile;
            NavigationContext.QueryString.TryGetValue("isTile", out isTile);

            if (!String.IsNullOrEmpty(isTile))
            {
                while (NavigationService.CanGoBack) NavigationService.RemoveBackEntry();
            }


            LoadProfile();
        }

        

        #region Profile

        private async void LoadProfile()
        {
            if (!this.Loading)
            {
                this.Loading = true;

             
                this.indicator = App.ShowLoading(this);
                String id;
                String isAssigned;
                String isKnown; 
                NavigationContext.QueryString.TryGetValue("friendid", out id);
                NavigationContext.QueryString.TryGetValue("assigned", out isAssigned);
                NavigationContext.QueryString.TryGetValue("isKnown", out isKnown);
                Boolean isKnownBoolean = false ;
                Boolean.TryParse(isKnown, out isKnownBoolean);
                username = id;

                 String isTile;
                NavigationContext.QueryString.TryGetValue("isTile", out isTile);

                if (String.IsNullOrEmpty(isTile))
                {

                    if (isKnownBoolean || (AppUser.Instance.Friends != null && AppUser.Instance.Friends.Contains(this.username)))
                    {
                        createUnFollowButton();
                    }
                    else
                    {
                        createFollowButton();
                    }
                }
                else
                {
                    createFollowButton();
                }

                App.FriendViewModel.NotifyPropertyChanged("ShowFollow");
                App.FriendViewModel.NotifyPropertyChanged("ShowUnFollow");

                TraktProfile profile = await userController.GetUserProfile(id);

                if (!String.IsNullOrEmpty(isAssigned) && isAssigned.Equals("true"))
                {
                    ToastNotification.ShowToast("Connected", "Your contact is now linked!");
                    NavigationService.RemoveBackEntry();
                }

                if (profile != null)
                {
                    App.FriendViewModel.Profile = profile;
                   
                    App.FriendViewModel.RefreshProfile();
                       this.Loading = false;
                       this.indicator.IsVisible = false;
                }
                else
                {
                    this.Loading = false;
                    this.indicator.IsVisible = false;
                }

            }
        }

        private void createUnFollowButton()
        {
            ApplicationBar appBar = new ApplicationBar();

            ApplicationBarIconButton followButton = new ApplicationBarIconButton(new Uri("Images/appbar.unseen.rest.png", UriKind.Relative));
            followButton.Click +=new EventHandler(unFollowButton_Click);
            followButton.Text = "follow";

            appBar.Buttons.Add(followButton);

            this.ApplicationBar = appBar;
        }

        private void createFollowButton()
        {
            ApplicationBar appBar = new ApplicationBar();

            ApplicationBarIconButton followButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            followButton.Click += new EventHandler(followButton_Click);
            followButton.Text = "unfollow";

            appBar.Buttons.Add(followButton);

            this.ApplicationBar = appBar;
        }

        private async void followButton_Click(object sender, EventArgs e)
        {
            if (await userController.followUser(this.username))
            {
                ToastNotification.ShowToast("User", "Following this user!");
                createUnFollowButton();
                AppUser.Instance.Friends.Add(this.username);
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private async void unFollowButton_Click(object sender, EventArgs e)
        {
            if (await userController.unFollowUser(this.username))
            {
                ToastNotification.ShowToast("User", "Unfollowing this user!");
                createFollowButton();
                AppUser.Instance.Friends.Remove(this.username);
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
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

                this.indicator = App.ShowLoading(this);

                App.FriendViewModel.clearHistory();

                String id;
                NavigationContext.QueryString.TryGetValue("friendid", out id);
                CreateHistoryList(await userController.getNewsFeed(id));
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
                    if (counter++ <= 20)
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
                            case "shout":
                                tempModel = Shout(activity);
                                break;
                        }

                        OrderHistory(activity, tempModel);
                    }


                }
                catch (NullReferenceException) { }


            }

            if (sortedOrderHistory != null)
            {
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

                        App.FriendViewModel.HistoryItems.Add(model);
                    }
                }
            }

            if (newsFeedActivity.Count == 0)
                ToastNotification.ShowToast("User", "News feed is empty!");

            App.FriendViewModel.NotifyPropertyChanged("HistoryItems");
            this.Loading = false;
            this.indicator.IsVisible = false;
        }


        private void OrderHistory(TraktActivity activity, ActivityListItemViewModel tempModel)
        {
            if (sortedOrderHistory == null)
            {
                sortedOrderHistory = new Dictionary<DateTime, List<ActivityListItemViewModel>>();
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

     

        private ActivityListItemViewModel Shout(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to movie " + activity.Movie.Title + "", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to show " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to episode " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Seen(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "saw " + activity.Movie.Title + ".", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "saw " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "saw " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        private ActivityListItemViewModel Scrobble(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "scrobbled " + activity.Movie.Title + "", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
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
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "checked in " + activity.Movie.Title, Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
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
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "rated " + activity.Movie.Title + ": " + activity.RatingAdvanced + "/10 ", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
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
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Movie.Title + " to the watchlist", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Show.Title + " to the watchlist.", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + "to the watchlist.", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Show.imdb_id, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }


        #endregion

        #region Taps
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


        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

       
        private void MainPanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           if (MainPanorama.SelectedIndex == 1)
            {
                if (App.FriendViewModel.HistoryItems.Count == 0)
                {
                    LoadHistoryData();
                }
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.FriendViewModel = null;
        }

        private void HistoryList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            ActivityListItemViewModel model = (ActivityListItemViewModel)e.Container.DataContext;
            model.LoadScreenImage();
        }

        private void HistoryList_ItemUnrealized(object sender, ItemRealizationEventArgs e)
        {
            ActivityListItemViewModel model = (ActivityListItemViewModel)e.Container.DataContext;
            model.ClearScreenImage();
            model.hasBeenUnrealized = true;
        }

   
    }
}
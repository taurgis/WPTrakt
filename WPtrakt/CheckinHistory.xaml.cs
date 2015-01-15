using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPtrakt.ViewModels;
using WPtrakt.Model.Trakt;
using WPtrakt.Controllers;
using WPtraktBase.Controller;
using WPtrakt.Model;

namespace WPtrakt
{
    public partial class CheckinHistory : PhoneApplicationPage
    {
        private Boolean Loading;
        private UserController userController;
        private ProgressIndicator indicator;
        public CheckinHistory()
        {
            InitializeComponent();

            userController = new UserController();
            this.DataContext = App.CheckinHistoryViewModel;
            this.Loaded += CheckinHistory_Loaded;
        }

        private void CheckinHistory_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;

            if (App.CheckinHistoryViewModel.HistoryItems.Count == 0)
            {
                LoadHistoryData();
            }
        }

        public async void LoadHistoryData()
        {
            if (!this.Loading)
            {
                this.Loading = true;

                indicator = App.ShowLoading(this);

                App.CheckinHistoryViewModel.ClearHistoryItems();

                CreateHistoryList(await userController.getCheckinHistory());

                this.Loading = false;
            }
        }

        private Dictionary<DateTime, List<ActivityListItemViewModel>> sortedOrderHistory;

        private void CreateHistoryList(List<TraktActivity> newsFeedActivity)
        {
            int counter = 0;
            sortedOrderHistory = null;
            newsFeedActivity.Sort(TraktActivity.ActivityComparison);
            foreach (TraktActivity activity in newsFeedActivity)
            {
                ActivityListItemViewModel tempModel = null;

                if (counter++ <= 60)
                {
                    switch (activity.Action)
                    {
                        case "checkin":
                            tempModel = Checkin(activity);
                            break;

                        case "scrobble":
                            tempModel = Scrobble(activity);
                            break;
                    }

                    if (tempModel != null)
                        OrderHistory(activity, tempModel);
                }




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

                        App.CheckinHistoryViewModel.HistoryItems.Add(model);
                    }
                }
            }

            if (newsFeedActivity.Count == 0)
                ToastNotification.ShowToast("User", "History list is empty!");

            App.CheckinHistoryViewModel.NotifyPropertyChanged("HistoryItems");
            indicator.IsVisible = false;
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

        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            if (!Loading)
            {
                App.CheckinHistoryViewModel.ClearHistoryItems();
                App.CheckinHistoryViewModel.NotifyPropertyChanged("HistoryItems");
                this.LoadHistoryData();
            }
        }

        private void PhoneApplicationPage_OrientationChanged_1(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                HistoryList.Width = 420;
            }
            else
            {
                HistoryList.Width = 680;
            }
        }

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
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
        }

    }


}
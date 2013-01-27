using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WPtrakt.Controllers;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controller;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public partial class ViewEpisode : PhoneApplicationPage
    {
        private EpisodeController episodeController;
        private ShowController showController;
        private TraktEpisode episode;
        private TraktShow show;
        private Boolean LoadingActive;

        public ViewEpisode()
        {
            InitializeComponent();
    
            DataContext = App.EpisodeViewModel;
            this.episodeController = new EpisodeController();
            this.showController = new ShowController();
            LoadingActive = false;
            this.Loaded += new RoutedEventHandler(ViewEpisode_Loaded);
        }

        private void EpisodePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.EpisodePanorama.SelectedIndex == 1)
            {
                if (!App.EpisodeViewModel.ShoutsLoading && App.EpisodeViewModel.ShoutItems.Count == 1)
                {
                    String id;
                    String season;
                    String episode;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    NavigationContext.QueryString.TryGetValue("season", out season);
                    NavigationContext.QueryString.TryGetValue("episode", out episode);
                    LoadShoutData(id, season, episode);
                }

                InitAppBarShouts();

            }
            else
            {
                InitAppBar();
            }
        }


        #region Load Episode

        private void ViewEpisode_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.EpisodeViewModel.IsDataLoaded)
            {
                LoadEpisode();
            }
        }

        private async void LoadEpisode()
        {
            if (!LoadingActive)
            {
                LoadingActive = true;
                String id;
                String season;
                String episodeNr;
                NavigationContext.QueryString.TryGetValue("id", out id);
                NavigationContext.QueryString.TryGetValue("season", out season);
                NavigationContext.QueryString.TryGetValue("episode", out episodeNr);

                LayoutRoot.Opacity = 1;

                show = await showController.getShowByTVDBID(id);
                episode = await episodeController.getEpisodeByTvdbAndSeasonInfo(id, season, episodeNr, show);
                if (episode != null)
                {
                    DateTime airTime = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                    airTime = airTime.AddSeconds(episode.FirstAired);

                    int daysSinceRelease = (DateTime.Now - airTime).Days;

                    App.EpisodeViewModel.UpdateEpisodeView(episode, show);
                    App.EpisodeViewModel.IsDataLoaded = true;
                    LoadBackgroundImage(show);
                    LoadScreenImage(episode);

                    InitAppBar();
                }
                LoadingActive = false;
            }
        }

        #endregion

        #region Load Images

        private async void LoadBackgroundImage(TraktShow show)
        {
            BitmapImage bgImage = await showController.getFanartImage(show.tvdb_id, show.Images.Fanart);
            EpisodePanorama.Background = new ImageBrush
            {
                ImageSource = bgImage,
                Opacity = 0.0,
                Stretch = Stretch.UniformToFill,
            };


        }


        private async void LoadScreenImage(TraktEpisode episode)
        {
            App.EpisodeViewModel.ScreenImage = await showController.getLargeScreenImage(episode.Tvdb, episode.Season, episode.Number, episode.Images.Screen);
        }
      
        #endregion

        #region Load shouts

        public async void LoadShoutData(String tvdb, String season, String episode)
        {
            App.EpisodeViewModel.clearShouts();
            App.EpisodeViewModel.addShout(new ListItemViewModel() { Name = "Loading..." });
            try
            {
                 TraktShout[] shouts = await this.episodeController.getShoutsForEpisode(tvdb,season,episode);
                 App.EpisodeViewModel.clearShouts();
             
                foreach (TraktShout shout in shouts)
                    App.EpisodeViewModel.addShout(new ListItemViewModel() { Name = shout.User.Username, ImageSource = shout.User.Avatar, SubItemText = shout.Shout });

                if (App.EpisodeViewModel.ShoutItems.Count == 0)
                    App.EpisodeViewModel.addShout(new ListItemViewModel() { Name = "No shouts" });

                App.EpisodeViewModel.ShoutsLoading = false;
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException)
            { ErrorManager.ShowConnectionErrorPopup(); }      
        }
     
        #endregion

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
            CreateRefreshEpisodesButton(appBar);
            this.ApplicationBar = appBar;
        }

        #region Refresh

        private void CreateRefreshEpisodesButton(ApplicationBar appBar)
        {
            ApplicationBarMenuItem refreshButton = new ApplicationBarMenuItem();
            refreshButton = new ApplicationBarMenuItem();
            refreshButton.Text = "Refresh";
            refreshButton.Click += new EventHandler(refreshButton_Click);

            appBar.MenuItems.Add(refreshButton);
        }

        void refreshButton_Click(object sender, EventArgs e)
        {
    
            App.EpisodeViewModel.Name = null;
            App.EpisodeViewModel.RefreshAll();
            showController.deleteEpisode(this.episode);
            LoadEpisode();
        }

        #endregion

        #region Watchlist

        private void CreateAddToWatchlist(ApplicationBar appBar)
        {
            ApplicationBarIconButton enabledAddtoWatchlist = new ApplicationBarIconButton();

            enabledAddtoWatchlist = new ApplicationBarIconButton(new Uri("Images/appbar.feature.video.rest.png", UriKind.Relative));
            enabledAddtoWatchlist.IsEnabled = true;
            enabledAddtoWatchlist.Click += new EventHandler(AddToWatchList_Click);
            enabledAddtoWatchlist.Text = "Watchlist +";
            appBar.Buttons.Add(enabledAddtoWatchlist);
        }

        private async void AddToWatchList_Click(object sender, EventArgs e)
        {
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;

            if (await episodeController.addEpisodeToWatchlist(this.show.tvdb_id, this.show.imdb_id, this.show.Title, this.show.year, this.episode.Season, this.episode.Number))
            {
                App.EpisodeViewModel.InWatchlist = true;

                await updateOtherViews();
                InitAppBar();
            }
            else
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

        private async void removeFromWatchlist_Click(object sender, EventArgs e)
        {
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;


            if (await episodeController.removeEpisodeFromWatchlist(this.show.tvdb_id, this.show.imdb_id, this.show.Title, this.show.year, this.episode.Season, this.episode.Number))
            {
                ToastNotification.ShowToast("Episode", "Episode removed from watchlist.");
                App.EpisodeViewModel.InWatchlist = false;
                await updateOtherViews();
                InitAppBar();
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        #endregion

        #region Checkin

        private void CreateCheckingButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton checkinButton = new ApplicationBarIconButton();
            checkinButton = new ApplicationBarIconButton(new Uri("Images/appbar.check.rest.png", UriKind.Relative));
            checkinButton.Text = "Check In";
            checkinButton.Click += new EventHandler(checkinButton_Click);

            appBar.Buttons.Add(checkinButton);
        }

        private async void checkinButton_Click(object sender, EventArgs e)
        {
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;

            try
            {
                if (await episodeController.checkinEpisode(this.show.tvdb_id, this.show.Title, this.show.year, this.episode.Season, this.episode.Number))
                   ToastNotification.ShowToast("Episode", "Checked in!");
                else
                   ToastNotification.ShowToast("Episode", "There is already a checkin in progress.");
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

        #region Seen

        private void CreateSeenButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(Seen_Click);

            appBar.Buttons.Add(watchedButton);
        }

        private async void Seen_Click(object sender, EventArgs e)
        {
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;

            if (await episodeController.markEpisodeAsSeen(this.show.tvdb_id, this.show.imdb_id, this.show.Title, this.show.year, this.episode.Season, this.episode.Number))
            {
                ToastNotification.ShowToast("Episode", "Episode marked as watched.");
                App.EpisodeViewModel.Watched = true;
                await updateOtherViews();
                InitAppBar();
            }
            else
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

        private async void unseeButton_Click(object sender, EventArgs e)
        {
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            if (await episodeController.unMarkEpisodeAsSeen(this.show.tvdb_id, this.show.imdb_id, this.show.Title, this.show.year, this.episode.Season, this.episode.Number))
            {
                App.EpisodeViewModel.Watched = false;
                await updateOtherViews();
                ToastNotification.ShowToast("Episode", "Episode unmarked as watched.");

                InitAppBar();
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        #endregion

        #region Rating

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
         
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=episode&imdb=" + this.show.imdb_id + "&tvdb=" + this.show.tvdb_id + "&year=" + App.EpisodeViewModel.ShowYear + "&title=" + App.EpisodeViewModel.ShowName + "&season=" + App.EpisodeViewModel.Season + "&episode=" + App.EpisodeViewModel.Number, UriKind.Relative));
        }

        #endregion

        private async System.Threading.Tasks.Task updateOtherViews()
        {
            this.episode = await episodeController.getEpisodeByTvdbAndSeasonInfo(this.show.tvdb_id, this.episode.Season, this.episode.Number, this.show);

            if (App.ShowViewModel != null && !String.IsNullOrEmpty(App.ShowViewModel.Tvdb) && App.ShowViewModel.Tvdb.Equals(this.show.tvdb_id))
            {
                App.ShowViewModel.updateEpisode(this.episode);
            }
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
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + this.show.tvdb_id, UriKind.Relative));
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

        #region Refresh shouts

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
          LoadShoutData(this.show.tvdb_id, App.EpisodeViewModel.Season, App.EpisodeViewModel.Number);
        }

        #endregion

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

                if (await episodeController.addShoutToEpisode(ShoutText.Text, this.show.tvdb_id, this.show.Title, this.show.year, this.episode.Season, this.episode.Number))
                {
                    ToastNotification.ShowToast("Episode", "Shout posted.");

                    ShoutText.Text = "";

                    this.Focus();
                }
                else
                {
                    ErrorManager.ShowConnectionErrorPopup();
                }
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
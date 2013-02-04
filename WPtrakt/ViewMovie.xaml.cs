using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public partial class ViewMovie : PhoneApplicationPage
    {
        private TraktMovie Movie { get; set; }
        private MovieController movieController;

        private void MoviePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MoviePanorama.SelectedIndex == 1)
            {
                if (!App.MovieViewModel.ShoutsLoading && App.MovieViewModel.ShoutItems.Count == 1)
                {
                    App.MovieViewModel.ShoutsLoading = true;
                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    LoadShoutData(id);
                }
                InitAppBarShouts();
            }
            else
            {
                InitAppBar();
            }
        }

        #region Load Movie

        public ViewMovie()
        {
            InitializeComponent();
            movieController = new MovieController();
            DataContext = App.MovieViewModel;
            this.Loaded += new RoutedEventHandler(ViewMovie_Loaded);
        }

        private async void ViewMovie_Loaded(object sender, RoutedEventArgs e)
        {
            String imdbId;
            NavigationContext.QueryString.TryGetValue("id", out imdbId);

            this.Movie = await movieController.getMovieByImdbId(imdbId);

            if (this.Movie != null)
            {
                if (!String.IsNullOrEmpty(this.Movie.GenresAsString))
                {
                    this.Movie.Genres = Movie.GenresAsString.Split('|');
                }

                App.MovieViewModel.UpdateMovieView(this.Movie);

                LoadBackgroundImage();
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        #endregion

        #region Load Shouts

        public async void LoadShoutData(String imdbId)
        {
            App.MovieViewModel.clearShouts();
            App.MovieViewModel.addShout(new ListItemViewModel() { Name = "Loading..." });

            TraktShout[] shouts = await this.movieController.getShoutsForMovie(this.Movie.imdb_id);
            App.MovieViewModel.clearShouts();

            foreach (TraktShout shout in shouts)
                App.MovieViewModel.addShout(new ListItemViewModel() { Name = shout.User.Username, ImageSource = shout.User.Avatar, Imdb = this.Movie.imdb_id, SubItemText = shout.Shout });

            if (App.MovieViewModel.ShoutItems.Count == 0)
                App.MovieViewModel.addShout(new ListItemViewModel() { Name = "No shouts" });

            App.MovieViewModel.ShoutsLoading = false;
        }

        #endregion

        #region Load Fanart

        private async void LoadBackgroundImage()
        {
            BitmapImage bgImage = await movieController.getFanartImage(this.Movie.imdb_id, this.Movie.Images.Fanart);
            this.MoviePanorama.Background = new ImageBrush
            {
                ImageSource = bgImage,
                Opacity = 0.0,
                Stretch = Stretch.UniformToFill,
            };

            Animation.ImageFadeIn(this.MoviePanorama.Background);
        }

        #endregion

        #region Taps

        private void ImdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://www.imdb.com/title/" + App.MovieViewModel.Imdb);

            task.Show();
        }

        private void TmdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://www.themoviedb.org/movie/" + App.MovieViewModel.Tmdb);

            task.Show();
        }

        private void TrailerButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {


            Regex Youtube = new Regex("youtu(?:\\.be|be\\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)");

            Match youtubeMatch = Youtube.Match(App.MovieViewModel.Trailer);

            string id = string.Empty;

            if (youtubeMatch.Success)
                id = youtubeMatch.Groups[1].Value;

            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri("http://www.youtube.com/embed/" + id + "?autoplay=1");
            webBrowserTask.Show();
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

            if (!App.MovieViewModel.InWatchlist)
                CreateAddToWatchlist(appBar);
            else
                CreateRemoveFromWatchlist(appBar); ;

            CreateRatingButton(appBar);

            if (App.MovieViewModel.Watched)
                CreateUnSeenButton(appBar);
            else
                CreateSeenButton(appBar);

            CreateCheckingButton(appBar);

            CreateTileMenuItem(appBar);

            this.ApplicationBar = appBar;
        }

        #region Tile

        private void CreateTileMenuItem(ApplicationBar appBar)
        {
            ApplicationBarMenuItem tileMenuItem = new ApplicationBarMenuItem();
            tileMenuItem.Text = "Pin to start";
            tileMenuItem.Click += new EventHandler(tileMenuItem_Click);
            appBar.MenuItems.Add(tileMenuItem);
        }

        void tileMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (StorageController.doesFileExist(App.MovieViewModel.Imdb + "background.jpg"))
                {
                    ImageController.copyImageToShellContent(App.MovieViewModel.Imdb + "background.jpg", App.MovieViewModel.Imdb);
                    StandardTileData NewTileData = new StandardTileData
                    {
                        BackgroundImage =
                             new Uri("isostore:/Shared/ShellContent/wptraktbg" + App.MovieViewModel.Imdb + ".jpg", UriKind.Absolute),
                        BackContent = App.MovieViewModel.Name,
                    };

                    ShellTile.Create(
                    new Uri(
                        "/ViewMovie.xaml?id=" + App.MovieViewModel.Imdb,
                        UriKind.Relative),
                        NewTileData);
                }
            }
            catch (InvalidOperationException) { ToastNotification.ShowToast("Tile", "Error creating tile, please try again!"); }
        }

        #endregion

        #region Rating

        private void CreateRatingButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton ratingButton = new ApplicationBarIconButton();
            ratingButton = new ApplicationBarIconButton(new Uri("Images/appbar.favs.rest.png", UriKind.Relative));
            ratingButton.IsEnabled = true;
            ratingButton.Text = "Rate";
            ratingButton.Click += new EventHandler(RateClick);

            appBar.Buttons.Add(ratingButton);
        }

        private void RateClick(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=movie&imdb=" + App.MovieViewModel.Imdb + "&year=" + App.MovieViewModel.Year + "&title=" + App.MovieViewModel.Name, UriKind.Relative));
        }

        #endregion

        #region Watchlist

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
            if (await this.movieController.removeMovieFromWatchlist(this.Movie.imdb_id, this.Movie.Title, this.Movie.year))
            {
                App.MovieViewModel.InWatchlist = false;
                ToastNotification.ShowToast("Movie", "Movie removed from watchlist.");
                InitAppBar();
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
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

        private async void AddToWatchList_Click(object sender, EventArgs e)
        {
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            if (await this.movieController.addMovieToWatchlist(this.Movie.imdb_id, this.Movie.Title, this.Movie.year))
            {
                App.MovieViewModel.InWatchlist = true;

                ToastNotification.ShowToast("Movie", "Movie added to watchlist.");

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

            if (await this.movieController.checkinMovie(this.Movie.imdb_id, this.Movie.Title, this.Movie.year))
            {
                App.MainPage.ShowWatchingNowMovie(this.Movie, DateTime.UtcNow);

                ToastNotification.ShowToast("Movie", "Checked in!");
            }
            else
                ToastNotification.ShowToast("Movie", "There is already a checkin in progress or connection problem!");

            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        #endregion Checkin

        #region Seen

        private void CreateSeenButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(SeenClick);

            appBar.Buttons.Add(watchedButton);
        }

        private async void SeenClick(object sender, EventArgs e)
        {
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            if (await movieController.markMovieAsSeen(this.Movie.imdb_id, this.Movie.Title, this.Movie.year))
            {
                ToastNotification.ShowToast("Movie", "Movie marked as watched.");
                App.MovieViewModel.Watched = true;
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
            if (await this.movieController.unMarkMovieAsSeen(this.Movie.imdb_id, this.Movie.Title, this.Movie.year))
            {
                ToastNotification.ShowToast("Movie", "Movie unmarked as watched.");
                App.MovieViewModel.Watched = false;

                InitAppBar();
            }
            else
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        #endregion

        #endregion

        #region Shouts AppBar

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
            LoadShoutData(App.MovieViewModel.Imdb);
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
                if (await this.movieController.addShoutToMovie(ShoutText.Text, this.Movie.imdb_id, this.Movie.Title, this.Movie.year))
                {
                    ToastNotification.ShowToast("Movie", "Shout posted.");
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
                this.MoviePanorama.Margin = new Thickness(0, 0, 0, 0);
                ShoutList.Width = 405;
                ShoutList.Height = 420;
            }
            else
            {
                this.MoviePanorama.Margin = new Thickness(0, -180, 0, 0);
                ShoutList.Width = 800;
                ShoutList.Height = 340;
            }

            MoviePanorama.DefaultItem = MoviePanorama.Items[0];
        }


        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.MovieViewModel = null;
            Animation.FadeOut(LayoutRoot);
        }
    }
}
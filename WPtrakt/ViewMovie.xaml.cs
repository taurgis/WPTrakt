﻿using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtraktBase.DAO;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public partial class ViewMovie : PhoneApplicationPage
    {
        public TraktMovie Movie { get; set; }

        private void MoviePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MoviePanorama.SelectedIndex == 1)
            {
                if (!App.MovieViewModel.ShoutsLoading)
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
            DataContext = App.MovieViewModel;
            this.Loaded += new RoutedEventHandler(ViewMovie_Loaded);
        }

        private void ViewMovie_Loaded(object sender, RoutedEventArgs e)
        {
            String imdbId;
            NavigationContext.QueryString.TryGetValue("id", out imdbId);

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = false;
            worker.WorkerSupportsCancellation = false;
            worker.DoWork += new DoWorkEventHandler(movieworker_DoWork);

            worker.RunWorkerAsync(imdbId);     
        }

        private async void movieworker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Movie = await MovieDao.Instance.getMovieByIMDB(e.Argument.ToString());
            this.Movie.Genres = Movie.GenresAsString.Split('|');

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                App.MovieViewModel.UpdateMovieView(this.Movie);
            });

            LoadBackgroundImage();
            
        }

        private void saveMovieToDB()
        {
            if (App.MovieViewModel != null)
                MovieDao.Instance.saveMovie(this.Movie);
        }

        #endregion

        #region Load Shouts

        public async void LoadShoutData(String imdbId)
        {
            App.MovieViewModel.clearShouts();
            App.MovieViewModel.addShout(new ListItemViewModel() { Name = "Loading..." });
            try
            {
                var movieClient = new WebClient();

                String jsonString = await movieClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + imdbId), AppUser.createJsonStringForAuthentication());

                App.MovieViewModel.clearShouts();

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                    TraktShout[] shouts = (TraktShout[])ser.ReadObject(ms);
                    foreach (TraktShout shout in shouts)
                        App.MovieViewModel.addShout(new ListItemViewModel() { Name = shout.User.Username, ImageSource = shout.User.Avatar, Imdb = this.Movie.imdb_id, SubItemText = shout.Shout });
                }

                if (App.MovieViewModel.ShoutItems.Count == 0)
                    App.MovieViewModel.addShout(new ListItemViewModel() { Name = "No shouts" });

                App.MovieViewModel.ShoutsLoading = false;
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
            String fileName = this.Movie.imdb_id + "background" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                   {
                       App.MovieViewModel.BackgroundImage = ImageController.getImageFromStorage(fileName);
                   }));
            }
            else
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(this.Movie.Images.Fanart));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;

                    System.Net.HttpStatusCode status = webResponse.StatusCode;

                    if (status == System.Net.HttpStatusCode.OK)
                    {
                        Stream str = webResponse.GetResponseStream();

                        Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            App.MovieViewModel.BackgroundImage = ImageController.saveImage(this.Movie.imdb_id + "background.jpg", str, 800, 450, 100);
                        }));
                    }
                }
                catch (WebException) { }
                catch (TargetInvocationException){ }
            }
        }

        #endregion


        #region Taps

        private void ImdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri( "http://www.imdb.com/title/" + App.MovieViewModel.Imdb);

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
            webBrowserTask.Uri = new Uri("http://www.youtube.com/embed/" + id +"?autoplay=1");
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
            auth.Movies = new TraktMovie[1];
            auth.Movies[0] = new TraktMovie();
            auth.Movies[0].imdb_id = App.MovieViewModel.Imdb;
            auth.Movies[0].Title = App.MovieViewModel.Name;
            auth.Movies[0].year = Int16.Parse(App.MovieViewModel.Year);

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadRemoveFromWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Movie", "Movie removed from watchlist.");

                this.Movie.InWatchlist = false;
                App.MovieViewModel.InWatchlist = false;
                saveMovieToDB();
        
                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
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

        private void AddToWatchList_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadWatchlistStringCompleted);
            WatchlistAuth auth = new WatchlistAuth();
            auth.Movies = new TraktMovie[1];
            auth.Movies[0] = new TraktMovie();
            auth.Movies[0].imdb_id = App.MovieViewModel.Imdb;
            auth.Movies[0].Title = App.MovieViewModel.Name;
            auth.Movies[0].year = Int16.Parse(App.MovieViewModel.Year);
            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                this.Movie.InWatchlist = true;
                App.MovieViewModel.InWatchlist = true;
                saveMovieToDB();

                ToastNotification.ShowToast("Movie", "Movie added to watchlist.");

                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void CreateCheckingButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton checkinButton = new ApplicationBarIconButton();
            checkinButton = new ApplicationBarIconButton(new Uri("Images/appbar.check.rest.png", UriKind.Relative));
            checkinButton.Text = "Check In";
            checkinButton.Click += new EventHandler(checkinButton_Click);

            appBar.Buttons.Add(checkinButton);
        }

        void checkinButton_Click(object sender, EventArgs e)
        {
            var checkinClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            checkinClient.UploadStringCompleted += new UploadStringCompletedEventHandler(checkinClient_UploadStringCompleted);
            CheckinAuth auth = new CheckinAuth();

            auth.imdb_id = App.MovieViewModel.Imdb;
            auth.Title = App.MovieViewModel.Name;
            auth.year = Int16.Parse(App.MovieViewModel.Year);
            auth.AppDate = AppUser.getReleaseDate();

            var assembly = Assembly.GetExecutingAssembly().FullName;
            var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];
            auth.AppVersion = fullVersionNumber;

            checkinClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));
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
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ShoutsIconButton_Click(object sender, EventArgs e)
        {
            LoadShoutData(App.MovieViewModel.Imdb);
        }

    
        private void CreateSeenButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(SeenClick);

            appBar.Buttons.Add(watchedButton);
        }

        private void SeenClick(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadSeenStringCompleted);
            WatchedAuth auth = new WatchedAuth();
            auth.Movies = new TraktMovieRequest[1];
            auth.Movies[0] = new TraktMovieRequest();
            auth.Movies[0].imdb_id = App.MovieViewModel.Imdb;
            auth.Movies[0].Title = App.MovieViewModel.Name;
            auth.Movies[0].year = Int16.Parse(App.MovieViewModel.Year);
            auth.Movies[0].Plays = 1;

            DateTime UnixEpoch =  new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            auth.Movies[0].LastPlayed = (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
        }

        void client_UploadSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                App.MovieViewModel.Watched = true;
                this.Movie.Watched = true;
                saveMovieToDB();

                ToastNotification.ShowToast("Movie", "Movie marked as watched.");

                InitAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }
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

        void unseeButton_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadUnSeenStringCompleted);
            WatchedAuth auth = new WatchedAuth();
            auth.Movies = new TraktMovieRequest[1];
            auth.Movies[0] = new TraktMovieRequest();
            auth.Movies[0].imdb_id = App.MovieViewModel.Imdb;
            auth.Movies[0].Title = App.MovieViewModel.Name;
            auth.Movies[0].year = Int16.Parse(App.MovieViewModel.Year);
            auth.Movies[0].Plays = 1;

            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            auth.Movies[0].LastPlayed = (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
        }

        void client_UploadUnSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Movie", "Movie unmarked as watched.");

                this.Movie.Watched = false;
                App.MovieViewModel.Watched = false;
                saveMovieToDB();

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
                watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadSendShoutStringCompleted);
                ShoutAuth auth = new ShoutAuth();

                auth.Imdb = App.MovieViewModel.Imdb;
                auth.Title = App.MovieViewModel.Name;
                auth.Year = Int16.Parse(App.MovieViewModel.Year);

                auth.Shout = (ShoutText.Text);

                watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/shout/movie/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
            }
        }

      
        void client_UploadSendShoutStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ToastNotification.ShowToast("Movie", "Shout posted.");

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
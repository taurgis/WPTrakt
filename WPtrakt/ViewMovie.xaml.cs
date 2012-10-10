using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using VPtrakt.Controllers;
using VPtrakt.Model.Trakt.Request;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;

namespace WPtrakt
{
    public partial class ViewMovie : PhoneApplicationPage
    {
        public ViewMovie()
        {
            InitializeComponent();

            DataContext = App.MovieViewModel;
            this.Loaded += new RoutedEventHandler(ViewMovie_Loaded);
        }

        private void ViewMovie_Loaded(object sender, RoutedEventArgs e)
        {
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.MovieViewModel.LoadData(id);
        }

        private void MoviePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MoviePanorama.SelectedIndex == 1)
            {
                if (!App.MovieViewModel.ShoutsLoaded)
                {
                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    App.MovieViewModel.LoadShoutData(id);
                }
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.MovieViewModel = null;
        }

        private void ImdbButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri( "http://www.imdb.com/title/" + App.MovieViewModel.Imdb);

            task.Show();
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
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
            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/watchlist/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth),auth));
        }

        private void ApplicationBarIconButton2_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=movie&imdb=" + App.MovieViewModel.Imdb + "&year=" + App.MovieViewModel.Year + "&title=" + App.MovieViewModel.Name, UriKind.Relative));
        }

        void client_UploadWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Movie added to watchlist.");
                LoadDisabledAddtoWatchlist();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LoadDisabledAddtoWatchlist()
        {
            ApplicationBar appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Minimized;

            ApplicationBarIconButton disabledAddtoWatchlist = new ApplicationBarIconButton();
            disabledAddtoWatchlist = new ApplicationBarIconButton(new Uri("Images/appbar.feature.video.rest.png", UriKind.Relative));
            disabledAddtoWatchlist.IsEnabled = false;
            disabledAddtoWatchlist.Text = "Watchlist +";
            appBar.Buttons.Add(disabledAddtoWatchlist);

            CreateRatingButton(appBar);
            CreateWatchedButton(appBar, !App.MovieViewModel.Watched);

            this.ApplicationBar = appBar;
        }

        private void ListBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!App.MovieViewModel.InWatchlist)
            {
                LoadEnabledAddtoWatchlist();
            }
            else
                LoadDisabledAddtoWatchlist();
        }

        private void LoadEnabledAddtoWatchlist()
        {
            ApplicationBar appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Minimized;

            ApplicationBarIconButton enabledAddtoWatchlist = new ApplicationBarIconButton();
            enabledAddtoWatchlist = new ApplicationBarIconButton(new Uri("Images/appbar.feature.video.rest.png", UriKind.Relative));
            enabledAddtoWatchlist.IsEnabled = true;
            enabledAddtoWatchlist.Click += new EventHandler(ApplicationBarIconButton_Click);
            enabledAddtoWatchlist.Text = "Watchlist +";
            appBar.Buttons.Add(enabledAddtoWatchlist);
            CreateRatingButton(appBar);
            CreateWatchedButton(appBar, !App.MovieViewModel.Watched);

            this.ApplicationBar = appBar;
        }

        private void CreateRatingButton(ApplicationBar appBar)
        {
            ApplicationBarIconButton ratingButton = new ApplicationBarIconButton();
            ratingButton = new ApplicationBarIconButton(new Uri("Images/appbar.favs.rest.png", UriKind.Relative));
            ratingButton.IsEnabled = true;
            ratingButton.Text = "Rate";
            ratingButton.Click += new EventHandler(ApplicationBarIconButton2_Click);

            appBar.Buttons.Add(ratingButton);
        }

        private void CreateWatchedButton(ApplicationBar appBar, Boolean enabled)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.IsEnabled = enabled;
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(ApplicationBarIconButton_Click_3);

            appBar.Buttons.Add(watchedButton);
        }


        private void ApplicationBarIconButton_Click_3(object sender, EventArgs e)
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

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/seen/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
        }

        void client_UploadSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Movie marked as watched.");
                App.MovieViewModel.Watched = true;
                if (App.MovieViewModel.InWatchlist)
                    LoadDisabledAddtoWatchlist();
                else
                {
                    LoadEnabledAddtoWatchlist();
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
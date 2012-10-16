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
using System.Windows.Media.Animation;
using System.IO.IsolatedStorage;
using System.Windows.Input;

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
                InitAppBarShouts();
            }
            else
            {
                if (!App.MovieViewModel.InWatchlist)
                {
                    LoadEnabledAddtoWatchlist();
                }
                else
                    LoadDisabledAddtoWatchlist();
            }

        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.MovieViewModel = null;
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandler = delegate { };
            completedHandler = delegate
            {
                storyboard.Completed -= completedHandler;
                storyboard.Stop();
            };
            storyboard.Completed += completedHandler;
            storyboard.Begin();
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
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktMovie.getFolderStatic() + "/" + App.MovieViewModel.Imdb + ".json");
    
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=movie&imdb=" + App.MovieViewModel.Imdb + "&year=" + App.MovieViewModel.Year + "&title=" + App.MovieViewModel.Name, UriKind.Relative));
        }

        void client_UploadWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Movie added to watchlist.");
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktMovie.getFolderStatic() + "/" + App.MovieViewModel.Imdb + ".json");
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


        private void InitAppBarShouts()
        {
            ApplicationBar appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Minimized;

            CreateRefreshShoutsButton(appBar);

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
            App.MovieViewModel.LoadShoutData(App.MovieViewModel.Imdb);
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
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktMovie.getFolderStatic() + "/" + App.MovieViewModel.Imdb + ".json");
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

        private void ShoutText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !String.IsNullOrEmpty(((TextBox)sender).Text))
            {
                var watchlistClient = new WebClient();
                watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadShoutStringCompleted);
                ShoutAuth auth = new ShoutAuth();

                auth.Imdb = App.MovieViewModel.Imdb;
                auth.Title = App.MovieViewModel.Name;
                auth.Year = Int16.Parse(App.MovieViewModel.Year);
               
                auth.Shout = ((TextBox)sender).Text;
                LastShout = auth.Shout;
                watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/shout/movie/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));

            }
        }
        private String LastShout { get; set; }
        void client_UploadShoutStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Shout posted. It might take up to a minute for the shout to show up in the application.");

                ShoutText.Text = "";

                this.Focus();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void ShoutText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ShoutText.Text.Equals("Press enter to submit."))
                ShoutText.Text = "";
        }

        private void ShoutText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ShoutText.Text))
                ShoutText.Text = "Press enter to submit.";
        }

    }
}
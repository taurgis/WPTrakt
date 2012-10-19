using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using VPtrakt.Controllers;
using VPtrakt.Model.Trakt.Request;
using WPtrakt.Model;
using WPtrakt.Model.Trakt.Request;
using System.IO.IsolatedStorage;
using WPtrakt.Model.Trakt;
using System.Windows.Navigation;

namespace WPtrakt
{
    public partial class ViewEpisode : PhoneApplicationPage
    {
        public ViewEpisode()
        {
            InitializeComponent();
    
            DataContext = App.EpisodeViewModel;
            this.Loaded += new RoutedEventHandler(ViewEpisode_Loaded);
        }

        private void ViewEpisode_Loaded(object sender, RoutedEventArgs e)
        {
            String id;
            String season;
            String episode;
            NavigationContext.QueryString.TryGetValue("id", out id);
            NavigationContext.QueryString.TryGetValue("season", out season);
            NavigationContext.QueryString.TryGetValue("episode", out episode);
            App.EpisodeViewModel.LoadData(id, season, episode);
        }

        private void EpisodePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.EpisodePanorama.SelectedIndex == 1)
            {
                if (!App.EpisodeViewModel.ShoutsLoaded)
                {
                    String id;
                    String season;
                    String episode;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    NavigationContext.QueryString.TryGetValue("season", out season);
                    NavigationContext.QueryString.TryGetValue("episode", out episode);
                    App.EpisodeViewModel.LoadShoutData(id, season, episode);
                }

                InitAppBarShouts();

            }
            else
            {
                LoadAppBar();
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.EpisodeViewModel = null;
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
            task.Uri = new Uri( "http://www.imdb.com/title/" + App.EpisodeViewModel.Imdb);

            task.Show();
        }

        private void ListBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LoadAppBar();
        }

        private void LoadAppBar()
        {
            ApplicationBar appBar = new ApplicationBar();

            CreateRatingButton(appBar);
            CreateWatchedButton(appBar, !App.EpisodeViewModel.Watched);

            this.ApplicationBar = appBar;
        }


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
            App.EpisodeViewModel.LoadShoutData(App.EpisodeViewModel.Tvdb, App.EpisodeViewModel.Season, App.EpisodeViewModel.Number);
        }

        private void CreateWatchedButton(ApplicationBar appBar, Boolean enabled)
        {
            ApplicationBarIconButton watchedButton = new ApplicationBarIconButton();
            watchedButton = new ApplicationBarIconButton(new Uri("Images/appbar.seen.rest.png", UriKind.Relative));
            watchedButton.IsEnabled = enabled;
            watchedButton.Text = "Seen";
            watchedButton.Click += new EventHandler(ApplicationBarIconButton_Click);

            appBar.Buttons.Add(watchedButton);
        }

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
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktWatched.getFolderStatic() + "/" + App.EpisodeViewModel.Tvdb + App.EpisodeViewModel.Season + App.EpisodeViewModel.Number  + ".json");
           
            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=episode&imdb=" + App.EpisodeViewModel.Imdb + "&year=" + App.EpisodeViewModel.ShowYear + "&title=" + App.EpisodeViewModel.ShowName + "&season=" + App.EpisodeViewModel.Season + "&episode=" + App.EpisodeViewModel.Number, UriKind.Relative));

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
                watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadShoutStringCompleted);
                ShoutAuth auth = new ShoutAuth();

                auth.Tvdb = App.EpisodeViewModel.Tvdb;
                auth.Title = App.EpisodeViewModel.ShowName;
                auth.Year = App.EpisodeViewModel.ShowYear;
                auth.Season = Int16.Parse(App.EpisodeViewModel.Season);
                auth.episode = Int16.Parse(App.EpisodeViewModel.Number);
                auth.Shout = (ShoutText.Text);
                LastShout = auth.Shout;
                watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/shout/episode/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadSeenStringCompleted);
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = App.EpisodeViewModel.Season;
            auth.Episodes[0].Episode = App.EpisodeViewModel.Number;
            auth.Imdb = App.EpisodeViewModel.Imdb;
            auth.Title = App.EpisodeViewModel.ShowName;
            auth.Year = App.EpisodeViewModel.ShowYear;

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));
        }

        void client_UploadSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Episode marked as watched.");
              
                App.EpisodeViewModel.Watched = true;
                LoadAppBar();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
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
    }
}
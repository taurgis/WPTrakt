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
            appBar.Mode = ApplicationBarMode.Minimized;

            CreateWatchedButton(appBar, !App.EpisodeViewModel.Watched);

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

            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/episode/seen/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));
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

        private void ShoutText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !String.IsNullOrEmpty(((TextBox)sender).Text))
            {
                var watchlistClient = new WebClient();
                watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadShoutStringCompleted);
                ShoutAuth auth = new ShoutAuth();
              
                auth.Tvdb = App.EpisodeViewModel.Tvdb;
                auth.Title = App.EpisodeViewModel.ShowName;
                auth.Year = App.EpisodeViewModel.ShowYear;
                auth.Season = Int16.Parse(App.EpisodeViewModel.Season);
                auth.episode = Int16.Parse( App.EpisodeViewModel.Number);
                auth.Shout = ((TextBox)sender).Text;
                LastShout = auth.Shout;
                watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/shout/episode/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));

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
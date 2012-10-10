using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using VPtrakt.Model.Trakt.Request;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtrakt.Model;
using VPtrakt.Controllers;

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

            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.EpisodeViewModel = null;
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
        
    }
}
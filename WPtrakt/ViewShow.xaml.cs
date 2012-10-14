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
using System.Collections.ObjectModel;
using VPtrakt.Model.Trakt.Request;
using WPtrakt.Model.Trakt;
using WPtrakt.Model;
using System.IO.IsolatedStorage;
using VPtrakt.Controllers;

namespace WPtrakt
{
    public partial class ViewShow : PhoneApplicationPage
    {
        private ApplicationBarIconButton previousSeason;
        private ApplicationBarIconButton nextSeason;

        public ViewShow()
        {
            InitializeComponent();

            DataContext = App.ShowViewModel;
            this.Loaded += new RoutedEventHandler(ViewShow_Loaded);
        }

        private void ViewShow_Loaded(object sender, RoutedEventArgs e)
        {
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.ShowViewModel.LoadData(id);
        }

        private void MoviePanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MoviePanorama.SelectedIndex == 0)
            {
                InitAppBarMain(false);
            }
            else if (this.MoviePanorama.SelectedIndex == 1)
            {
                if (App.ShowViewModel.EpisodeItems.Count == 0)
                {
                    this.ApplicationBar.IsVisible = true;

                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    App.ShowViewModel.LoadEpisodeData(id);
                }
                InitAppBarSeasons();

            }
            else if (this.MoviePanorama.SelectedIndex == 2)
            {
                if (!App.ShowViewModel.ShoutsLoaded)
                {
                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    App.ShowViewModel.LoadShoutData(id);
                }
                this.ApplicationBar.IsVisible = false;

            }
            else
            {
                this.ApplicationBar.IsVisible = false;
            }
        }

        private void InitAppBarSeasons()
        {
            ApplicationBar appBar = new ApplicationBar();

            previousSeason = new ApplicationBarIconButton(new Uri("Images/appbar.back.rest.png", UriKind.Relative));
            previousSeason.Click += new EventHandler(ApplicationBarIconButton_Click_EpisodeBack);
            previousSeason.Text = "Previous";

            if (App.ShowViewModel.currentSeason == 1)
                previousSeason.IsEnabled = false;
            else
                previousSeason.IsEnabled = true;

            appBar.Buttons.Add(previousSeason);

            nextSeason = new ApplicationBarIconButton(new Uri("Images/appbar.next.rest.png", UriKind.Relative));
            nextSeason.Click += new EventHandler(ApplicationBarIconButton_Click_EpisodeForward);

            if (App.ShowViewModel.currentSeason == App.ShowViewModel.numberOfSeasons)
            {
                nextSeason.IsEnabled = false;
            }
            else
                nextSeason.IsEnabled = true;
          
            nextSeason.Text = "Next";
            appBar.Buttons.Add(nextSeason);
            this.ApplicationBar = appBar;
        }

        private void InitAppBarMain(Boolean forceDisabled)
        {
            ApplicationBar appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Minimized;
            ApplicationBarIconButton disabledAddtoWatchlist = new ApplicationBarIconButton();
            disabledAddtoWatchlist = new ApplicationBarIconButton(new Uri("Images/appbar.feature.video.rest.png", UriKind.Relative));

            if (App.ShowViewModel.InWatchlist || forceDisabled)
            {
                disabledAddtoWatchlist.IsEnabled = false;
            }

            disabledAddtoWatchlist.Click += new EventHandler(disabledAddtoWatchlist_Click);

            disabledAddtoWatchlist.Text = "Watchlist +";
            appBar.Buttons.Add(disabledAddtoWatchlist);

            ApplicationBarIconButton ratingButton = new ApplicationBarIconButton();
            ratingButton = new ApplicationBarIconButton(new Uri("Images/appbar.favs.rest.png", UriKind.Relative));
            ratingButton.IsEnabled = true;
            ratingButton.Text = "Rate";
            ratingButton.Click += new EventHandler(ratingButton_Click);

            appBar.Buttons.Add(ratingButton);

            this.ApplicationBar = appBar;
        }

        void ratingButton_Click(object sender, EventArgs e)
        {
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktShow.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + ".json");

            NavigationService.Navigate(new Uri("/RatingSelector.xaml?type=show&imdb=" + App.ShowViewModel.Imdb + "&year=" + App.ShowViewModel.Year + "&title=" + App.ShowViewModel.Name, UriKind.Relative));
        }

        void disabledAddtoWatchlist_Click(object sender, EventArgs e)
        {
            var watchlistClient = new WebClient();
            progressBarLoading.Visibility = System.Windows.Visibility.Visible;
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadWatchlistStringCompleted);
            WatchlistAuth auth = new WatchlistAuth();
            auth.Shows = new TraktShow[1];
            auth.Shows[0] = new TraktShow();
            auth.Shows[0].imdb_id = App.ShowViewModel.Imdb;
            auth.Shows[0].Title = App.ShowViewModel.Name;
            auth.Shows[0].year = Int16.Parse(App.ShowViewModel.Year);
            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/show/watchlist/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Show added to watchlist.");
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktShow.getFolderStatic() + "/" + App.ShowViewModel.Tvdb + ".json");
                InitAppBarMain(true);
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            progressBarLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.ShowViewModel = null;
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
            task.Uri = new Uri( "http://www.imdb.com/title/" + App.ShowViewModel.Imdb);

            task.Show();
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };
            completedHandlerMainPage = delegate
            {

                StackPanel senderPanel = (StackPanel)sender;
                ListItemViewModel model = (ListItemViewModel)senderPanel.DataContext;
                NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };
            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
                 }

        private void PanoramaItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

                String id;
                NavigationContext.QueryString.TryGetValue("id", out id);
                App.ShowViewModel.LoadEpisodeData(id);
            
        }


        private void ApplicationBarIconButton_Click_EpisodeBack(object sender, EventArgs e)
        {
            App.ShowViewModel.currentSeason -= 1;
            String id;
            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.ShowViewModel.LoadEpisodeData(id);
            InitAppBarSeasons();
        }

        private void ApplicationBarIconButton_Click_EpisodeForward(object sender, EventArgs e)
        {
            App.ShowViewModel.currentSeason += 1;
            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.ShowViewModel.LoadEpisodeData(id);

            InitAppBarSeasons();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 1;
        }

        private void ListBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitAppBarMain(false);
        }
    


        
    }
}
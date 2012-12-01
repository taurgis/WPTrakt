using Clarity.Phone.Controls;
using Microsoft.Phone.Controls;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;

namespace WPtrakt
{
    public partial class MyMovies : PhoneApplicationPage
    {
        public MyMovies()
        {
            InitializeComponent();
            DataContext = App.MyMoviesViewModel;
            this.Loaded += new RoutedEventHandler(MyMoviesPage_Loaded);

            this.Filter.SelectedIndex = AppUser.Instance.MyMoviesFilter;
        }

        private void MyMoviesPage_Loaded(object sender, RoutedEventArgs e)
        {

            if (!App.MyMoviesViewModel.IsDataLoaded)
            {
                if (this.Filter.SelectedIndex == 0)
                {
                    App.MyMoviesViewModel.LoadData();
                }
                else if (this.Filter.SelectedIndex == 1)
                {
                    App.MyMoviesViewModel.LoadMyWatchListMoviesData();
                }
            }
        }

        #region Taps

        private void Canvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
             ListItemViewModel model = (ListItemViewModel)((Canvas)sender).DataContext;
             Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Image)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        #endregion

        #region ApplicationBar

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (this.MyMoviesPanorama.SelectedIndex == 0)
            {
                if (this.Filter.SelectedIndex == 0)
                {
                    StorageController.DeleteFile("mymovies.json");
                    App.MyMoviesViewModel.LoadData();
                }
                else if (this.Filter.SelectedIndex == 1)
                {
                    StorageController.DeleteFile("mywatchlistmovies.json");
                    App.MyMoviesViewModel.LoadMyWatchListMoviesData();
                }
            }
            else
            {
               App.MyMoviesViewModel.LoadSuggestData();
            }
        }

        #endregion

        #region MovieContextMenu

        private ListItemViewModel lastModel;

        private void SeenMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;
            var seenClient = new WebClient();

            seenClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMovieSeenStringCompleted);
            WatchedAuth auth = new WatchedAuth();
            auth.Movies = new TraktMovieRequest[1];
            auth.Movies[0] = new TraktMovieRequest();
            auth.Movies[0].imdb_id = lastModel.Imdb;
            auth.Movies[0].Title = lastModel.Name;
            auth.Movies[0].year = Int16.Parse(lastModel.SubItemText);
            auth.Movies[0].Plays = 1;

            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            auth.Movies[0].LastPlayed = (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;

            seenClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
        }

        private void client_UploadMovieSeenStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                lastModel.Watched = true;
                MessageBox.Show("Movie marked as watched.");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null;
        }

        private void WatchlistMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;
            var watchlistClient = new WebClient();
            watchlistClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMovieWatchlistStringCompleted);
            WatchlistAuth auth = new WatchlistAuth();
            auth.Movies = new TraktMovie[1];
            auth.Movies[0] = new TraktMovie();
            auth.Movies[0].imdb_id = lastModel.Imdb;
            auth.Movies[0].Title = lastModel.Name;
            auth.Movies[0].year = Int16.Parse(lastModel.SubItemText);
            watchlistClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
        }

        void client_UploadMovieWatchlistStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                lastModel.InWatchList = true;
                MessageBox.Show("Movie added to watchlist.");

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null;
        }

        private void CheckinMovie_Click(object sender, RoutedEventArgs e)
        {
            lastModel = (ListItemViewModel)((MenuItem)sender).DataContext;

            var checkinClient = new WebClient();
            checkinClient.UploadStringCompleted += new UploadStringCompletedEventHandler(checkinClient_UploadStringCompleted);
            CheckinAuth auth = new CheckinAuth();

            auth.imdb_id = lastModel.Imdb;
            auth.Title = lastModel.Name;
            auth.year = Int16.Parse(lastModel.SubItemText);
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
                    MessageBox.Show("There is already a checkin in progress.");
                else
                    MessageBox.Show("Checked in!");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            lastModel = null;
        }

        #endregion

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MyMoviesPanorama.SelectedIndex == 1)
            {
                if (App.MyMoviesViewModel.SuggestItems.Count == 0)
                {
                    App.MyMoviesViewModel.LoadSuggestData();
                }
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                ListSuggestions.Width = 700;

            }
            else
            {
                ListSuggestions.Width = 1370;
            }
        }
        private Int32 lastSelection;
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Filter != null)
            {
                if (this.lastSelection != this.Filter.SelectedIndex)
                {
                    this.lastSelection = this.Filter.SelectedIndex;
                    AppUser.Instance.MyMoviesFilter = this.Filter.SelectedIndex;
                    App.MyMoviesViewModel.FilterMovies(this.Filter.SelectedIndex);
                }
            }
        }
    }

    public class MovieNameSelector : IQuickJumpGridSelector
    {
        public Func<object, IComparable> GetGroupBySelector()
        {
            return (p) => ((ListItemViewModel)p).Name.FirstOrDefault();
        }

        public Func<object, string> GetOrderByKeySelector()
        {
            return (p) => ((ListItemViewModel)p).Name;
        }

        public Func<object, string> GetThenByKeySelector()
        {
            return (p) => (string.Empty);
        }
    }
}
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using WPtrakt.Controllers;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;

namespace WPtrakt
{
    public partial class Main : PhoneApplicationPage
    {
        public Main()
        {
            InitializeComponent();
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                ToastNotification.ShowToast("Connection", "No connection available!");
                return;
            }

            if (!App.ViewModel.IsDataLoaded)
            {
                if (String.IsNullOrEmpty(AppUser.Instance.UserName) || String.IsNullOrEmpty(AppUser.Instance.Password))
                {
                    NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
                }
                else
                {
                    App.ViewModel.LoadData();
                }
            }
        }

        private void MainPanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPanorama.SelectedIndex == 1)
            {
                if (App.ViewModel.TrendingItems.Count == 0)
                {
                    App.ViewModel.loadTrending();
                }
            }
        }

        #region Taps

        private void ApplicationBarRefreshButton_Click(object sender, EventArgs e)
        {
            if ((MainPanorama.SelectedIndex == 0) || (MainPanorama.SelectedIndex == 2))
            {
                StorageController.DeleteFile(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json");
                App.ViewModel.LoadData();
            }
            else if (MainPanorama.SelectedIndex == 1)
            {
                App.ViewModel.loadTrending();
            }
        }

        private void MyMovies(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot,  new Uri("/MyMovies.xaml", UriKind.Relative));
        }

        private void MyShows_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/MyShows.xaml", UriKind.Relative));
        }

        private void Search_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/Search.xaml", UriKind.Relative));
        }

        private void TrendingImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Image)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        private void HistoryListItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Panel)sender).DataContext;
          
            if (model.Type.Equals("episode"))
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));

            else
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        #endregion

        #region Menu

        private void CancelCheckin_Click(object sender, EventArgs e)
        {
            var cancelCheckinClient = new WebClient();

            cancelCheckinClient.UploadStringCompleted += new UploadStringCompletedEventHandler(cancelCheckinClient_UploadStringCompleted);
            cancelCheckinClient.UploadStringAsync(new Uri("http://api.trakt.tv/movie/cancelcheckin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication());
        }

        void cancelCheckinClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                ToastNotification.ShowToast("Cancel", "Cancelled any active check in!");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void FriendActivity_Click(object sender, EventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/FriendActivity.xaml", UriKind.Relative));
        }


        #endregion

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                this.MainPanorama.Margin = new Thickness(0, 0, 0, 0);
                ListTrending.Width = 700;
                HistoryList.Height = 520;
            }
            else
            {
                if (e.Orientation == PageOrientation.LandscapeRight)
                {
                    this.MainPanorama.Margin = new Thickness(40, -180, 0, 0);
                }
                else
                {
                    this.MainPanorama.Margin = new Thickness(0, -180, 0, 0);
                }

                ListTrending.Width = 1370;
                HistoryList.Height = 400;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }
    }
}
using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using Coding4Fun.Phone.Controls;
using System.Net;
using VPtrakt.Model.Trakt.Request;
using VPtrakt.Controllers;

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
                var toast = new ToastPrompt
                {
                    Title = "Connection",
                    TextOrientation = System.Windows.Controls.Orientation.Vertical,
                    Message = "No connection available!",
                };
                toast.Show();
                return;
            }

            if (!App.ViewModel.IsDataLoaded)
            {
                if (String.IsNullOrEmpty(AppUser.Instance.UserName) || String.IsNullOrEmpty(AppUser.Instance.Password))
                {
                    Uri theUri = new Uri("/Settings.xaml", UriKind.Relative);
                    NavigationService.Navigate(theUri);
                }
                else
                    App.ViewModel.LoadData();
            }
        }

        #region Taps

        private void ApplicationBarSettingsButton_Click(object sender, EventArgs e)
        {
            Uri theUri = new Uri("/Settings.xaml", UriKind.Relative);
            NavigationService.Navigate(theUri);
        }

        private void ApplicationBarRefreshButton_Click(object sender, EventArgs e)
        {
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json");
            App.ViewModel.Profile = null;
            App.ViewModel.LoadData();
        }

        private void MyMovies(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };

            completedHandlerMainPage = delegate
            {
                Uri theUri = new Uri("/MyMovies.xaml", UriKind.Relative);
                NavigationService.Navigate(theUri);
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };

            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
        }

        private void MyShows_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };

            completedHandlerMainPage = delegate
            {
                Uri theUri = new Uri("/MyShows.xaml", UriKind.Relative);
                NavigationService.Navigate(theUri);
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };

            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
        }

        private void StackPanel_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Uri theUri = new Uri("/Search.xaml", UriKind.Relative);
            NavigationService.Navigate(theUri);
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };

            completedHandlerMainPage = delegate
            {

                Image senderImage = (Image)sender;
                ListItemViewModel model = (ListItemViewModel)senderImage.DataContext;
                NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };

            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
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
                if(model.Type.Equals("episode"))
                    NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                else
                    NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
              
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };

            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
        }

        #endregion

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 1;
        }

        private void MainPanorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPanorama.SelectedIndex == 1)
            {
                if (App.ViewModel.TrendingItems.Count == 0)
                {
                    if (!App.ViewModel.LoadingTrendingItems)
                    {
                        App.ViewModel.LoadingTrendingItems = true;
                        App.ViewModel.loadTrending();
                    }
                }
            }
        }
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
                String jsonString = e.Result;
                var toast = new ToastPrompt
                {
                    Title = "Cancel",
                    TextOrientation = System.Windows.Controls.Orientation.Vertical,
                    Message = "Cancelled any active check in!",
                };

                toast.Show();
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
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
                this.MainPanorama.Margin = new Thickness(0, -180, 0, 0);
                ListTrending.Width = 1300;
                HistoryList.Height = 400;
            }
        }
    }
}
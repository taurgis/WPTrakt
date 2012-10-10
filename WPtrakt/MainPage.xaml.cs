using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using WPtrakt.Model;

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
                MessageBox.Show("No network connection available!");
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
                {
                    App.ViewModel.LoadData();
                }
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
            App.ViewModel.Profile = null;
            App.ViewModel.LoadData();
        }

        private void MyMovies(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Uri theUri = new Uri("/MyMovies.xaml", UriKind.Relative);
            NavigationService.Navigate(theUri);
        }

        private void MyShows_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Uri theUri = new Uri("/MyShows.xaml", UriKind.Relative);
            NavigationService.Navigate(theUri);
        }

        private void StackPanel_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Uri theUri = new Uri("/Search.xaml", UriKind.Relative);
            NavigationService.Navigate(theUri);
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Image senderImage = (Image)sender;
            ListItemViewModel model =  (ListItemViewModel)senderImage.DataContext;
            NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel senderPanel = (StackPanel)sender;
            ListItemViewModel model = (ListItemViewModel)senderPanel.DataContext;
            NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
        }

        #endregion

       

    }
}
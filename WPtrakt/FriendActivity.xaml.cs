using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using WPtrakt.ViewModels;

namespace WPtrakt
{
    public partial class FriendActivity : PhoneApplicationPage
    {
        public FriendActivity()
        {
            InitializeComponent();
            DataContext = App.ActivityViewModel;

            this.Loaded += new RoutedEventHandler(FriendActivity_Loaded);
        }

        void FriendActivity_Loaded(object sender, RoutedEventArgs e)
        {
            if(App.ActivityViewModel.Activity == null)
             App.ActivityViewModel.LoadData();
        }

        private void ActivityGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };

            completedHandlerMainPage = delegate
            {
                Grid activityGrid = (Grid)sender;
                ActivityListItemViewModel model = (ActivityListItemViewModel)activityGrid.DataContext;

                if (model.Type.Equals("episode"))
                    NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                else if (model.Type.Equals("movie"))
                    NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
                else
                    NavigationService.Navigate(new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };

            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();

          
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            App.ActivityViewModel.LoadData();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 1;
        }
    }
}
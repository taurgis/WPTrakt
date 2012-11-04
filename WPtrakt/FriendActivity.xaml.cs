using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;
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
            LayoutRoot.Opacity = 1;
            if(App.ActivityViewModel.Activity == null)
             App.ActivityViewModel.LoadData();
        }

        private void ActivityGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ActivityListItemViewModel model = (ActivityListItemViewModel)((Grid)sender).DataContext;

            Uri redirectUri = null;
            if (model.Type.Equals("episode"))
                redirectUri = new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative);
            else if (model.Type.Equals("movie"))
                redirectUri = new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative);
            else
                redirectUri = new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative);

            Animation.NavigateToFadeOut(this, LayoutRoot, redirectUri);
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            App.ActivityViewModel.LoadData();
        }

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }
    }
}
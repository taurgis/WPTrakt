using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Windows.Phone.PersonalInformation;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controller;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public partial class Friends : PhoneApplicationPage
    {
        private Boolean Loading;
        private UserController userController;
     

        public Friends()
        {
            InitializeComponent();
            this.Loading = false;
            this.userController = new UserController();
            DataContext = App.FriendsViewModel;
        }

        public async void LoadData()
        {
            if (!this.Loading)
            {
                String id;
                NavigationContext.QueryString.TryGetValue("id", out id);

              
                if (!String.IsNullOrEmpty(id))
                {
                    while (NavigationService.CanGoBack) NavigationService.RemoveBackEntry();
                }


                this.Loading = true;

                this.progressBar.Visibility = System.Windows.Visibility.Visible;

                App.FriendsViewModel.clearResult();
                AppUser.Instance.Friends = new List<string>();

                LoadFollowingAsync();
                LoadFollowersAsync();
                List<TraktProfile> friends = await userController.getFriends();
              

                foreach (TraktProfile friend in friends)
                {
                    App.FriendsViewModel.ResultItems.Add(new ListItemViewModel() { Name = friend.Username, ImageSource = friend.Avatar });
                    AppUser.Instance.Friends.Add(friend.Username);
                }

                App.FriendsViewModel.NotifyPropertyChanged("ResultItems");

              

                this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
                this.Loading = false;
            }
        }

        private async void LoadFollowingAsync()
        {
            List<TraktProfile> following = await userController.getFollowing();

            foreach (TraktProfile friend in following)
            {
                App.FriendsViewModel.FollowingResultItems.Add(new ListItemViewModel() { Name = friend.Username, ImageSource = friend.Avatar });
                AppUser.Instance.Friends.Add(friend.Username);
            }

            App.FriendsViewModel.NotifyPropertyChanged("FollowingResultItems");
        }

        private async void LoadFollowersAsync()
        {
            List<TraktProfile> followers = await userController.getFollowers();


            foreach (TraktProfile friend in followers)
            {
                App.FriendsViewModel.FollowersResultItems.Add(new ListItemViewModel() { Name = friend.Username, ImageSource = friend.Avatar });
             
            }

            App.FriendsViewModel.NotifyPropertyChanged("FollowersResultItems");
        }


        private void MovieCanvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
          
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
  
            LoadData();
        }

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }

        private async void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Grid)sender).DataContext;

            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);

            if (!String.IsNullOrEmpty(id))
            {
                ContactBindingManager bindingManager = await ContactBindings.GetAppContactBindingManagerAsync();
                ContactBinding entity = await bindingManager.GetContactBindingByRemoteIdAsync(model.Name); ;

                await bindingManager.CreateContactBindingTileAsync(id, entity);

                NavigationService.Navigate(new Uri("/Friend.xaml?friendid=" + model.Name + "&assigned=true" + "&isKnown=true", UriKind.Relative));
            }
            else
            {
    
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/Friend.xaml?friendid=" + model.Name + "&isKnown=true", UriKind.Relative));
            }
        }


        private void Grid_Tap2(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Grid)sender).DataContext;

            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/Friend.xaml?friendid=" + model.Name + "&isKnown=false", UriKind.Relative));
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/SearchUsers.xaml", UriKind.Relative));
        }
    }
}
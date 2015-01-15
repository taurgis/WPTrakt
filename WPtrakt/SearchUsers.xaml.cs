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
    public partial class SearchUsers : PhoneApplicationPage
    {
        private Boolean Loading;
        private UserController userController;
     

        public SearchUsers()
        {
            InitializeComponent();
            this.Loading = false;
            this.userController = new UserController();
            DataContext = App.SearchUsersViewModel;
        }

        public async void DoSearch()
        {
            if (!this.Loading)
            {
                this.Loading = true;

                this.progressBar.Visibility = System.Windows.Visibility.Visible;

                App.SearchUsersViewModel.clearResult();
                App.TrackEvent("UserSearch", "Searching for " + SearchText.Text);
                List<TraktProfile> friends = await userController.SearchUsers(SearchText.Text);
              

                foreach (TraktProfile friend in friends)
                {
                    App.SearchUsersViewModel.ResultItems.Add(new ListItemViewModel() { Name = friend.Username, ImageSource = friend.Avatar });
                }

                App.SearchUsersViewModel.NotifyPropertyChanged("ResultItems");

                this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
                this.Loading = false;
            }
        }


        private void SearchText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchText.Text.Equals("Search..."))
                SearchText.Text = "";
        }

        private void SearchText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchText.Text.Equals(""))
                SearchText.Text = "Search...";
        }

        private void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (SearchText.Text.Length > 1)
                {
                    DoSearch();
                    this.Focus();
                }
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Grid)sender).DataContext;

            NavigationService.Navigate(new Uri("/Friend.xaml?friendid=" + model.Name + "&isKnown=false", UriKind.Relative));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoSearch();
        }
    }
}
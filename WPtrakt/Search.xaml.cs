using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using WPtrakt.Controllers;
using WPtrakt.Model;

namespace WPtrakt
{
    public partial class Search : PhoneApplicationPage
    {
        public Search()
        {
            InitializeComponent();
            DataContext = App.SearchViewModel;
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
                    App.SearchViewModel.LoadData(SearchText.Text);
                    this.Focus();
                }
            }
        }

        private void MovieCanvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Canvas)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        private void ShowCanvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Canvas)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }

        #region "Speech Recognition"

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New && NavigationContext.QueryString.ContainsKey("voiceCommandName"))
            {
                if (String.IsNullOrEmpty(AppUser.Instance.AppVersion) && (String.IsNullOrEmpty(AppUser.Instance.UserName) || String.IsNullOrEmpty(AppUser.Instance.Password)))
                {
                    await Speech.Speak(string.Format("You have not configured a user yet!"));
                    NavigationService.GoBack();
                }

                String searchString;
                if (null != (searchString = await LookForSomething(NavigationContext.QueryString)))
                {
                    if (searchString.Length > 1)
                    {
                        this.SearchText.Text = searchString;
                        App.SearchViewModel.LoadData(searchString);
                        this.Focus();
                    }
                }
               
            }
            else if (e.NavigationMode == NavigationMode.Back && !System.Diagnostics.Debugger.IsAttached)
            {
                NavigationService.GoBack();
            }
        }

        private async Task<string> LookForSomething(IDictionary<string, string> queryString)
        {
            await Speech.Speak(string.Format("What are you looking for?"));
            return await Speech.GetResult("Ex. \"The matrix\"");
        }

        #endregion
    }
}
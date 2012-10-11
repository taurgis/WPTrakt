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
            if (this.MoviePanorama.SelectedIndex == 1)
            {
                if (App.ShowViewModel.EpisodeItems.Count == 0)
                {
                   this.ApplicationBar.IsVisible = true;
                  
                    String id;
                    NavigationContext.QueryString.TryGetValue("id", out id);
                    App.ShowViewModel.LoadEpisodeData(id);
                }
                InitAppBar();

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

        private void InitAppBar()
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
            StackPanel senderPanel = (StackPanel)sender;
            ListItemViewModel model = (ListItemViewModel)senderPanel.DataContext;
            NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
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
            InitAppBar();
        }

        private void ApplicationBarIconButton_Click_EpisodeForward(object sender, EventArgs e)
        {
            App.ShowViewModel.currentSeason += 1;
            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            String id;
            NavigationContext.QueryString.TryGetValue("id", out id);
            App.ShowViewModel.LoadEpisodeData(id);

            InitAppBar();
        }
    


        
    }
}
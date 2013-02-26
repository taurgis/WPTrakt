using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPtraktBase.Controller;
using WPtraktBase.Model.Trakt;
using WPtrakt.Controllers;
using WPtrakt.ViewModels;

namespace WPtrakt
{
    public partial class ViewTrending : PhoneApplicationPage
    {
        private MovieController movieController;
        private ProgressIndicator indicator;
        public ViewTrending()
        {
            InitializeComponent();

            this.DataContext = App.TrendingViewModel;

            this.Loaded += ViewTrending_Loaded;
        }

        private async void ViewTrending_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
            if (App.TrendingViewModel.TrendingItems.Count == 0)
            {
                indicator = App.ShowLoading(this);
              
                movieController = new MovieController();

                TraktMovie[] movies = await movieController.GetTrendingMovies();
                App.TrendingViewModel.ClearTrendingItems();
                foreach (TraktMovie movie in movies)
                {
                    App.TrendingViewModel.TrendingItems.Add(new ViewModels.TrendingListItemViewModel() { Imdb = movie.imdb_id, Name = movie.Title, Year = movie.year, ImageSource = movie.Images.Poster });
                    App.TrendingViewModel.NotifyPropertyChanged("TrendingItems");
                }

                indicator.IsVisible = false;
            }

        }

        private void Grid_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            TrendingListItemViewModel model = (TrendingListItemViewModel)((StackPanel)sender).DataContext;

            Uri redirectUri = null;
            redirectUri = new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative);

            Animation.NavigateToFadeOut(this, LayoutRoot, redirectUri);

        }

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }
    }
}
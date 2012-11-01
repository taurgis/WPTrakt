using Clarity.Phone.Controls;
using Microsoft.Phone.Controls;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;

namespace WPtrakt
{
    public partial class MyMovies : PhoneApplicationPage
    {
        public MyMovies()
        {
            InitializeComponent();
            DataContext = App.MyMoviesViewModel;
            this.Loaded += new RoutedEventHandler(MyMoviesPage_Loaded);
        }

        private void MyMoviesPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.MyMoviesViewModel.IsDataLoaded)
            {
                App.MyMoviesViewModel.LoadData();
            }
        }

        #region Taps

        private void Canvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
             ListItemViewModel model = (ListItemViewModel)((Canvas)sender).DataContext;
             Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Image)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        #endregion

        #region ApplicationBar

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (this.MyMoviesPanorama.SelectedIndex == 0)
            {
                StorageController.DeleteFile("mymovies.json");
                App.MyMoviesViewModel.LoadData();
            }
            else
            {
               App.MyMoviesViewModel.LoadSuggestData();
            }
        }

        #endregion

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MyMoviesPanorama.SelectedIndex == 1)
            {
                if (App.MyMoviesViewModel.SuggestItems.Count == 0)
                {
                    App.MyMoviesViewModel.LoadSuggestData();
                }
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                this.MyMoviesPanorama.Margin = new Thickness(0, 0, 0, 0);
                ListSuggestions.Width = 700;
                ListMyMovies.Height = 590;
            }
            else
            {
                if (e.Orientation == PageOrientation.LandscapeRight)
                {
                    this.MyMoviesPanorama.Margin = new Thickness(50, -180, 0, 0);
                }
                else
                {
                    this.MyMoviesPanorama.Margin = new Thickness(0, -180, 0, 0);
                }

                ListSuggestions.Width = 1370;
                ListMyMovies.Height = 480;
            }
        }
    }

    public class MovieNameSelector : IQuickJumpGridSelector
    {
        public Func<object, IComparable> GetGroupBySelector()
        {
            return (p) => ((ListItemViewModel)p).Name.FirstOrDefault();
        }

        public Func<object, string> GetOrderByKeySelector()
        {
            return (p) => ((ListItemViewModel)p).Name;
        }

        public Func<object, string> GetThenByKeySelector()
        {
            return (p) => (string.Empty);
        }
    }
}
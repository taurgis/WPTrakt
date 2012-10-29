﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clarity.Phone.Controls;
using Microsoft.Phone.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.Windows.Media.Animation;

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
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };
            completedHandlerMainPage = delegate
            {

                Canvas senderImage = (Canvas)sender;
                ListItemViewModel model = (ListItemViewModel)senderImage.DataContext;
                NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };
            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
        }

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MyMoviesPanorama.SelectedIndex == 1)
            {
                if (App.MyMoviesViewModel.SuggestItems.Count == 0)
                {
                    if (!App.MyMoviesViewModel.LoadingSuggestItems)
                    {
                        App.MyMoviesViewModel.LoadingSuggestItems = true;
                        App.MyMoviesViewModel.LoadSuggestData();
                    }
                }
            }
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

        #endregion

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (this.MyMoviesPanorama.SelectedIndex == 0)
            {
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("mymovies.json");

                App.MyMoviesViewModel.LoadData();
            }
            else
            {
                if (!App.MyMoviesViewModel.LoadingSuggestItems)
                {
                    App.MyMoviesViewModel.LoadingSuggestItems = true;
                    App.MyMoviesViewModel.LoadSuggestData();
                } 
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
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

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 1;
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
using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Clarity.Phone.Controls;
using Microsoft.Phone.Controls;
using WPtrakt.Controllers;

namespace WPtrakt
{
    public partial class MyShows : PhoneApplicationPage
    {
        public MyShows()
        {
            InitializeComponent();
            DataContext = App.MyShowsViewModel;
            this.Loaded += new RoutedEventHandler(MyShowsPage_Loaded);
        }

        private void MyShowsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.MyShowsViewModel.IsDataLoaded)
            {
                App.MyShowsViewModel.LoadData();
            }
        }

        #region Taps

        private void Canvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Canvas)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((StackPanel)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
        }


        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Image)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
        }

        #endregion

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (this.MyShowsPanorama.SelectedIndex == 0)
            {
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("myshows.json");

                App.MyShowsViewModel.LoadData();
            }
            else if (this.MyShowsPanorama.SelectedIndex == 1)
            {
                App.MyShowsViewModel.LoadCalendarData();
            }
            else if (this.MyShowsPanorama.SelectedIndex == 2)
            {
                App.MyShowsViewModel.LoadSuggestData();
            }
        }

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MyShowsPanorama.SelectedIndex == 1)
            {
                if (App.MyShowsViewModel.CalendarItems.Count == 0)
                {
                    App.MyShowsViewModel.LoadCalendarData();
                }
            }
            else if (this.MyShowsPanorama.SelectedIndex == 2)
            {
                if (App.MyShowsViewModel.SuggestItems.Count == 0)
                {
                    App.MyShowsViewModel.LoadSuggestData();
                }
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.PortraitDown) || (e.Orientation == PageOrientation.PortraitUp))
            {
                this.MyShowsPanorama.Margin = new Thickness(0, 0, 0, 0);
                ListSuggestions.Width = 700;
                ListMyShows.Height = 590;
                ListUpcomming.Height = 519;
            }
            else
            {
                if (e.Orientation == PageOrientation.LandscapeRight)
                {
                    this.MyShowsPanorama.Margin = new Thickness(50, -180, 0, 0);
                }
                else
                {
                    this.MyShowsPanorama.Margin = new Thickness(0, -180, 0, 0);
                }

                ListSuggestions.Width = 1370;
                ListMyShows.Height = 480;
                ListUpcomming.Height = 425;
            }
        }
    }

    public class ShowNameSelector : IQuickJumpGridSelector
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
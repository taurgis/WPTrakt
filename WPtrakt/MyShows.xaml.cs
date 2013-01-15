using Clarity.Phone.Controls;
using Microsoft.Phone.Controls;
using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;
using WPtrakt.Model;

namespace WPtrakt
{
    public partial class MyShows : PhoneApplicationPage
    {
        public MyShows()
        {
            InitializeComponent();
            DataContext = App.MyShowsViewModel;
            this.Loaded += new RoutedEventHandler(MyShowsPage_Loaded);

            this.Filter.SelectedIndex = AppUser.Instance.MyShowsFilter;
        }

        private void MyShowsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.MyShowsViewModel.IsDataLoaded)
            {
                if (this.Filter.SelectedIndex == 0)
                {
                    App.MyShowsViewModel.LoadData();
                }
                else if (this.Filter.SelectedIndex == 1)
                {
                    App.MyShowsViewModel.LoadMyWatchListShowsData();
                } 
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
                if (this.Filter.SelectedIndex == 0)
                {
                    IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("myshows.json");
                    App.MyShowsViewModel.LoadData();
                }
                else if (this.Filter.SelectedIndex == 1)
                {
                    StorageController.DeleteFile("mywatchlistshows.json");
                    App.MyShowsViewModel.LoadMyWatchListShowsData();
                }
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
                ListSuggestions.Width = 700;
            }
            else
            {
                ListSuggestions.Width = 1370;
            }
        }

        private Int32 lastSelection;
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Filter != null)
            {
                if (this.lastSelection != this.Filter.SelectedIndex)
                {
                    this.lastSelection = this.Filter.SelectedIndex;
                    AppUser.Instance.MyShowsFilter = this.Filter.SelectedIndex;
                    App.MyShowsViewModel.FilterShows(this.Filter.SelectedIndex);
                }
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
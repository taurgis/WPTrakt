using Microsoft.Phone.Controls;
using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WPtrakt.Controllers;
using WPtrakt.Model;
using System.Windows.Navigation;
using WPtraktBase.Controllers;

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
                App.MyShowsViewModel.Indicator = App.ShowLoading(this); 
                if (this.Filter.SelectedIndex == 0)
                {
                    this.AllText.Visibility = System.Windows.Visibility.Visible;
                    this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
                    this.SeenText.Visibility = System.Windows.Visibility.Collapsed;
                    this.UnSeenText.Visibility = System.Windows.Visibility.Collapsed;
                    App.MyShowsViewModel.LoadData(this.Filter.SelectedIndex);
                }
                else if (this.Filter.SelectedIndex == 1)
                {
                    this.AllText.Visibility = System.Windows.Visibility.Collapsed;
                    this.WatchlistText.Visibility = System.Windows.Visibility.Visible;
                    this.SeenText.Visibility = System.Windows.Visibility.Collapsed;
                    this.UnSeenText.Visibility = System.Windows.Visibility.Collapsed;
                }
                else if (this.Filter.SelectedIndex == 2)
                {
                    this.AllText.Visibility = System.Windows.Visibility.Collapsed;
                    this.SeenText.Visibility = System.Windows.Visibility.Visible;
                    this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
                    App.MyShowsViewModel.LoadData(this.Filter.SelectedIndex);
                }
                else if (this.Filter.SelectedIndex == 3)
                {
                    this.AllText.Visibility = System.Windows.Visibility.Collapsed;
                    this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
                    this.UnSeenText.Visibility = System.Windows.Visibility.Visible;
                    App.MyShowsViewModel.LoadData(this.Filter.SelectedIndex);
                } 
            }
        }

        #region Taps

        private void Canvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Canvas)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
        }
        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((Image)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
        }

        private void AllText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.Filter.Open();
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
                App.MyShowsViewModel.Indicator = App.ShowLoading(this);
                App.MyShowsViewModel.fullprogress = null;

                if (this.Filter.SelectedIndex == 0 || this.Filter.SelectedIndex >= 2)
                {
                    App.MyShowsViewModel.LoadData(this.Filter.SelectedIndex);
                }
                else if (this.Filter.SelectedIndex == 1)
                {
                    App.MyShowsViewModel.LoadMyWatchListShowsData();
                }
            }
            else if (this.MyShowsPanorama.SelectedIndex == 1)
            {
                App.MyShowsViewModel.Indicator = App.ShowLoading(this);
                App.MyShowsViewModel.LoadSuggestData();
            }
        }

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.TrackEvent("MyShows", "Switched panorama to " + this.MyShowsPanorama.SelectedIndex);
            if (this.MyShowsPanorama.SelectedIndex == 1)
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
                App.TrackEvent("MyShows", "Switched filter to " + this.Filter.SelectedIndex);

                if (this.lastSelection != this.Filter.SelectedIndex)
                {
                    this.lastSelection = this.Filter.SelectedIndex;
                    AppUser.Instance.MyShowsFilter = this.Filter.SelectedIndex;
                    if (this.Filter.SelectedIndex == 0)
                    {
                        this.AllText.Visibility = System.Windows.Visibility.Visible;
                        this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
                        this.SeenText.Visibility = System.Windows.Visibility.Collapsed;
                        this.UnSeenText.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else if (this.Filter.SelectedIndex == 1)
                    {
                        this.AllText.Visibility = System.Windows.Visibility.Collapsed;
                        this.WatchlistText.Visibility = System.Windows.Visibility.Visible;
                        this.SeenText.Visibility = System.Windows.Visibility.Collapsed;
                        this.UnSeenText.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else if (this.Filter.SelectedIndex == 2)
                    {
                        this.UnSeenText.Visibility = System.Windows.Visibility.Collapsed;
                        this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
                        this.AllText.Visibility = System.Windows.Visibility.Collapsed;
                        this.SeenText.Visibility = System.Windows.Visibility.Visible;
                    }
                    else if (this.Filter.SelectedIndex == 3)
                    {
                        this.SeenText.Visibility = System.Windows.Visibility.Collapsed;
                        this.WatchlistText.Visibility = System.Windows.Visibility.Collapsed;
                        this.AllText.Visibility = System.Windows.Visibility.Collapsed;
                        this.UnSeenText.Visibility = System.Windows.Visibility.Visible;
                    }

                    App.MyShowsViewModel.FilterShows(this.Filter.SelectedIndex);
                }
            }
        }
    }
}
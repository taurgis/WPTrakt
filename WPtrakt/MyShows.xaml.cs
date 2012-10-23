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
using Clarity.Phone.Controls;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;

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
                    if (!App.MyShowsViewModel.LoadingSuggestItems)
                    {
                        App.MyShowsViewModel.LoadingSuggestItems = true;
                        App.MyShowsViewModel.LoadSuggestData();
                    }
                }
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
                Canvas senderCanvas = (Canvas)sender;
                ListItemViewModel model = (ListItemViewModel)senderCanvas.DataContext;
                NavigationService.Navigate(new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };
            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };
            completedHandlerMainPage = delegate
            {
                StackPanel senderPanel = (StackPanel)sender;
                ListItemViewModel model = (ListItemViewModel)senderPanel.DataContext;
                NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };
            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();
        
        }


        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
            Storyboard.SetTarget(storyboard, LayoutRoot);
            EventHandler completedHandlerMainPage = delegate { };
            completedHandlerMainPage = delegate
            {
                Image senderCanvas = (Image)sender;
                ListItemViewModel model = (ListItemViewModel)senderCanvas.DataContext;
                NavigationService.Navigate(new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
                storyboard.Completed -= completedHandlerMainPage;
                storyboard.Stop();
                this.Opacity = 0;
            };
            storyboard.Completed += completedHandlerMainPage;
            storyboard.Begin();

        }

        #endregion

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

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("myshows.json");

            App.MyShowsViewModel.LoadData();
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
                this.MyShowsPanorama.Margin = new Thickness(0, -180, 0, 0);
                ListSuggestions.Width = 1300;
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
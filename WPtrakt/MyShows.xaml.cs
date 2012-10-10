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

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.MyShowsViewModel = null;
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

        #region Taps

        private void Canvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Canvas senderCanvas = (Canvas)sender;
            ListItemViewModel model = (ListItemViewModel)senderCanvas.DataContext;
            NavigationService.Navigate(new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel senderPanel = (StackPanel)sender;
            ListItemViewModel model = (ListItemViewModel)senderPanel.DataContext;
            NavigationService.Navigate(new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
        }


        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Image senderImage = (Image)sender;
            ListItemViewModel model = (ListItemViewModel)senderImage.DataContext;
            NavigationService.Navigate(new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));

        }

        #endregion
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
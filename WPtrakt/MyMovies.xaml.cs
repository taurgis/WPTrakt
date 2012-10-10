using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Clarity.Phone.Controls;
using Microsoft.Phone.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;

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

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
           App.MyMoviesViewModel = null;
        }

        #region Taps

        private void Canvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Canvas senderCanvas = (Canvas)sender;
            ListItemViewModel model = (ListItemViewModel)senderCanvas.DataContext;
            NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

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

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Image senderImage = (Image)sender;
            ListItemViewModel model = (ListItemViewModel)senderImage.DataContext;
            NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        #endregion

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            IsolatedStorageFile.GetUserStoreForApplication().DeleteFile("mymovies.json");
 
            App.MyMoviesViewModel.LoadData();
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
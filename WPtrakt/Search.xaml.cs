using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Phone.Controls;
using WPtrakt;

namespace VPtrakt
{
    public partial class Search : PhoneApplicationPage
    {

        public Search()
        {
            InitializeComponent();
            DataContext = App.SearchViewModel;
            this.Loaded += new RoutedEventHandler(SearchPage_Loaded);
        }

        private void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void SearchText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchText.Text.Equals("Search..."))
                SearchText.Text = "";
        }

        private void SearchText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchText.Text.Equals(""))
                SearchText.Text = "Search...";
        }

        private void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (SearchText.Text.Length > 1)
                {
                    App.SearchViewModel.LoadData(SearchText.Text);
                    this.Focus();
                }
            }
        }

        private void MovieCanvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Canvas senderCanvas = (Canvas)sender;
            ListItemViewModel model = (ListItemViewModel)senderCanvas.DataContext;
            NavigationService.Navigate(new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
        }

        private void ShowCanvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Canvas senderCanvas = (Canvas)sender;
            ListItemViewModel model = (ListItemViewModel)senderCanvas.DataContext;
            NavigationService.Navigate(new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
        }
    }
}
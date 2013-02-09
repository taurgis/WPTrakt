using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPtrakt;
using WPtrakt.Controllers;
using WPtraktBase.Controller;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public partial class Search : PhoneApplicationPage
    {
        private Boolean Loading;
        private ShowController showController;
        private MovieController movieController;

        public Search()
        {
            InitializeComponent();
            this.Loading = false;
            this.showController = new ShowController();
            this.movieController = new MovieController();
            DataContext = App.SearchViewModel;
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
                    LoadData(SearchText.Text);
                    this.Focus();
                }
            }
        }

        public async void LoadData(String searchTerm)
        {
            if (!this.Loading)
            {
                this.Loading = true;

                this.progressBar.Visibility = System.Windows.Visibility.Visible;

                App.SearchViewModel.clearResult();

                TraktShow[] shows = await showController.searchForShows(searchTerm);
                TraktMovie[] movies = await movieController.searchForMovies(searchTerm);

                List<Object> mergedList = new List<object>();

                Boolean isShow = true;
                Int16 showLocation = 0;
                Int16 movieLocation = 0;
                for (int i = 0; i < shows.Length + movies.Length; i++)
                {
                    if (isShow)
                    {
                        if (showLocation < shows.Length)
                        {
                            mergedList.Add(shows[showLocation++]);
                        }

                        isShow = false;
                    }
                    else
                    {
                        if (movieLocation < movies.Length)
                        {
                            mergedList.Add(movies[movieLocation++]);
                        }

                        isShow = true;
                    }

                }


                foreach (Object result in mergedList)
                {
                    if (result.GetType() == typeof(TraktShow))
                    {
                        TraktShow show = (TraktShow)result;
                        App.SearchViewModel.ResultItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id, SubItemText = show.year.ToString(), Genres = show.Genres, Type = "show" });
             
                    }
                    else
                    {
                        TraktMovie movie = (TraktMovie)result;
                        App.SearchViewModel.ResultItems.Add(new ListItemViewModel() {  Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres, Type = "movie" });
                    }
                }

                App.SearchViewModel.NotifyPropertyChanged("ResultItems");

                this.progressBar.Visibility = System.Windows.Visibility.Collapsed;
                this.Loading = false;
            }
        }


        private void MovieCanvas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((TextBlock)sender).DataContext;

            if(model.Type.Equals("movie"))
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewMovie.xaml?id=" + model.Imdb, UriKind.Relative));
            else
                Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewShow.xaml?id=" + model.Tvdb, UriKind.Relative));
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
        }

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }
    }
}
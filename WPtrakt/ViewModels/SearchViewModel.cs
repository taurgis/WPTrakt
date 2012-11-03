using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;


namespace WPtrakt
{
    public class SearchViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> MovieItems { get; private set; }
        public ObservableCollection<ListItemViewModel> ShowItems  { get; private set; }


        public SearchViewModel()
        {
           
            _loadingStatus = "Collapsed";
            NotifyPropertyChanged("LoadingStatus");
        }

        private String _loadingStatus;
        public String LoadingStatus
        {
            get
            {  
               return _loadingStatus;
            }
        }

        public void LoadData(String key)
        {
            _loadingStatus = "Visible";
            NotifyPropertyChanged("LoadingStatus");
            key = RemoveDiacritics(key);
            this.MovieItems = null;
            this.ShowItems = null;
            NotifyPropertyChanged("MovieItems");
            NotifyPropertyChanged("ShowItems");

            var movieClient = new WebClient();
            movieClient.Encoding = Encoding.GetEncoding("UTF-8");
            movieClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMovieStringCompleted);
            movieClient.UploadStringAsync(new Uri("http://api.trakt.tv/search/movies.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + key), AppUser.createJsonStringForAuthentication());

            var showClient = new WebClient();
            showClient.Encoding = Encoding.GetEncoding("UTF-8");
            showClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadShowsStringCompleted);
            showClient.UploadStringAsync(new Uri("http://api.trakt.tv/search/shows.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + key), AppUser.createJsonStringForAuthentication());
        }

        static char[] frenchReplace = { 'a', 'a', 'a', 'a', 'c', 'e', 'e', 'e', 'e', 'i', 'i', 'o', 'o', 'u', 'u', 'u', '+' };
        static char[] frenchAccents = { 'à', 'â', 'ä', 'æ', 'ç', 'é', 'è', 'ê', 'ë', 'î', 'ï', 'ô', 'œ', 'ù', 'û', 'ü', ' ' };

        public static string RemoveDiacritics(string accentedStr)
        {
            char[] replacement = frenchReplace;
            char[] accents = frenchAccents;

            if (accents != null && replacement != null && accentedStr.IndexOfAny(accents) > -1)
            {
             
                for (int i = 0; i < accents.Length; i++)
                {
                   accentedStr = accentedStr.Replace(accents[i], replacement[i]);
                }

                return accentedStr;
            }
            else
                return accentedStr;
        } 

        void client_UploadMovieStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MovieItems = new ObservableCollection<ListItemViewModel>();
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                    TraktMovie[] movies = (TraktMovie[])ser.ReadObject(ms);
                    foreach (TraktMovie movie in movies)
                    {
                        this.MovieItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres, Type = "Movie" });
                    }
                    if (ShowItems != null)
                    {
                        _loadingStatus = "Collapsed";
                        NotifyPropertyChanged("LoadingStatus");
                        NotifyPropertyChanged("ShowItems");
                        NotifyPropertyChanged("MovieItems");
                    }
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        void client_UploadShowsStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ShowItems = new ObservableCollection<ListItemViewModel>();
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShow[]));
                    TraktShow[] shows = (TraktShow[])ser.ReadObject(ms);

                    foreach (TraktShow show in shows)
                    {
                        this.ShowItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id, SubItemText = show.year.ToString(), Genres = show.Genres, Type="Show" });
                    }
                    if (MovieItems != null)
                    {
                        _loadingStatus = "Collapsed";
                        NotifyPropertyChanged("LoadingStatus");
                        NotifyPropertyChanged("ShowItems");
                        NotifyPropertyChanged("MovieItems");
                    }
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
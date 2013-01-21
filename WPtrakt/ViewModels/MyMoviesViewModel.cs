using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;


namespace WPtrakt
{
    public class MyMoviesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> MovieItems { get; private set; }
        public ObservableCollection<ListItemViewModel> SuggestItems { get; private set; }
        private Boolean LoadingSuggestItems { get; set; }
        private Boolean LoadingMovies { get; set; }

        public MyMoviesViewModel()
        {
            this.MovieItems = new ObservableCollection<ListItemViewModel>();
            this.SuggestItems = new ObservableCollection<ListItemViewModel>();

        }

        #region Getters/Setters

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public String LoadingStatus
        {
            get
            {
                if (this.LoadingMovies || this.LoadingSuggestItems)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        #endregion

        #region MyMovies

        public void LoadData()
        {
            if (!this.LoadingMovies)
            {
                this.LoadingMovies = true;
                this.MovieItems = new ObservableCollection<ListItemViewModel>();
                RefreshMyMoviesView();
                String fileName = "mymovies.json";
                if (StorageController.doesFileExist(fileName))
                {
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = false;
                    worker.WorkerSupportsCancellation = false;
                    worker.DoWork += new DoWorkEventHandler(myMoviesworker_DoWork);

                    worker.RunWorkerAsync();
                }
                else
                {
                    CallMyMoviesService();
                }


                this.IsDataLoaded = true;
            }
        }

        void myMoviesworker_DoWork(object sender, DoWorkEventArgs e)
        {
            String fileName = "mymovies.json";
            if ((DateTime.Now - IsolatedStorageFile.GetUserStoreForApplication().GetLastWriteTime(fileName)).Days < 1)
            {
                TraktMovie[] myMovies = (TraktMovie[])StorageController.LoadObjectFromMain(fileName, typeof(TraktMovie[]));
                ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();

                foreach (TraktMovie movie in myMovies)
                {
                    tempItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres });
                }

                if (tempItems.Count == 0)
                {
                    tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
                }

                this.LoadingMovies = false;

                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   UpdateMyMovieView(tempItems);
               });
            }
            else
            {
                CallMyMoviesService();
            }
        }

        private void CallMyMoviesService()
        {
            HttpWebRequest request;

            request = (HttpWebRequest)WebRequest.Create(new Uri("https://api.trakt.tv/user/library/movies/all.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
            request.Method = "POST";
            request.BeginGetRequestStream(new AsyncCallback(GetMyMoviesRequestStreamCallback), request);
        }

        void GetMyMoviesRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadMyMoviesStringCompleted), webRequest);
        }

        void client_DownloadMyMoviesStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                System.Net.HttpStatusCode status = httpResoponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    String jsonString = new StreamReader(httpResoponse.GetResponseStream()).ReadToEnd();

                    ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();
                    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                    {
                        //parse into jsonser
                        var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                        TraktMovie[] obj = (TraktMovie[])ser.ReadObject(ms);
                        StorageController.saveObjectInMainFolder(obj, typeof(TraktMovie[]), "mymovies.json");
                        foreach (TraktMovie movie in obj)
                        {
                            tempItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres });
                        }

                        if (tempItems.Count == 0)
                        {
                            tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
                        }

                        this.LoadingMovies = false;

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            UpdateMyMovieView(tempItems);
                        });
                    }
                }

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException)
            { ErrorManager.ShowConnectionErrorPopup(); }
        }

        private void UpdateMyMovieView(ObservableCollection<ListItemViewModel> tempItems)
        {
            this.MovieItems = tempItems;
            RefreshMyMoviesView();
        }

        private void RefreshMyMoviesView()
        {
            NotifyPropertyChanged("MovieItems");
            NotifyPropertyChanged("LoadingStatus");
        }

        public void FilterMovies(int type)
        {
             if (type == 0)
            {
                this.LoadData();
            }
            else if (type == 1)
            {
                this.LoadMyWatchListMoviesData();
            }
        }

        #endregion

        #region MyWatchListMovies

        public void LoadMyWatchListMoviesData()
        {
            if (!this.LoadingMovies)
            {
                this.LoadingMovies = true;
                this.MovieItems = new ObservableCollection<ListItemViewModel>();
                RefreshMyMoviesView();
                String fileName = "mywatchlistmovies.json";
                if (StorageController.doesFileExist(fileName))
                {
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = false;
                    worker.WorkerSupportsCancellation = false;
                    worker.DoWork += new DoWorkEventHandler(myWatchListMoviesworker_DoWork);

                    worker.RunWorkerAsync();
                }
                else
                {
                    CallMyWatchListMoviesService();
                }
            }
        }

        void myWatchListMoviesworker_DoWork(object sender, DoWorkEventArgs e)
        {
            String fileName = "mywatchlistmovies.json";
            if ((DateTime.Now - IsolatedStorageFile.GetUserStoreForApplication().GetLastWriteTime(fileName)).Days < 1)
            {
                TraktMovie[] myMovies = (TraktMovie[])StorageController.LoadObjectFromMain(fileName, typeof(TraktMovie[]));
                ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();

                foreach (TraktMovie movie in myMovies)
                {
                    tempItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres });
                }

                if (tempItems.Count == 0)
                {
                    tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
                }

                this.LoadingMovies = false;

                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateMyMovieView(tempItems);
                });
            }
            else
            {
                CallMyWatchListMoviesService();
            }
        }

        private void CallMyWatchListMoviesService()
        {
            HttpWebRequest request;

            request = (HttpWebRequest)WebRequest.Create(new Uri("https://api.trakt.tv/user/watchlist/movies.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
            request.Method = "POST";
            request.BeginGetRequestStream(new AsyncCallback(GetMyWatchListMoviesRequestStreamCallback), request);
        }

        void GetMyWatchListMoviesRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadMyWatchListMoviesStringCompleted), webRequest);
        }

        void client_DownloadMyWatchListMoviesStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                System.Net.HttpStatusCode status = httpResoponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    String jsonString = new StreamReader(httpResoponse.GetResponseStream()).ReadToEnd();

                    ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();
                    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                    {
                        //parse into jsonser
                        var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                        TraktMovie[] obj = (TraktMovie[])ser.ReadObject(ms);
                        StorageController.saveObjectInMainFolder(obj, typeof(TraktMovie[]), "mywatchlistmovies.json");
                        foreach (TraktMovie movie in obj)
                        {
                            tempItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres });
                        }

                        if (tempItems.Count == 0)
                        {
                            tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
                        }

                        this.LoadingMovies = false;

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            UpdateMyMovieView(tempItems);
                        });
                    }
                }

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException)
            { ErrorManager.ShowConnectionErrorPopup(); }
        }

        #endregion

        #region Suggest

        public void LoadSuggestData()
        {
            if (!this.LoadingSuggestItems)
            {
                this.LoadingSuggestItems = true;
                this.SuggestItems = new ObservableCollection<ListItemViewModel>();
                NotifyPropertyChanged("SuggestItems");
                NotifyPropertyChanged("LoadingStatus");
                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri("https://api.trakt.tv/recommendations/movies/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
                request.Method = "POST";
                request.BeginGetRequestStream(new AsyncCallback(GetSuggestionsRequestStreamCallback), request);
            }
        }

        void GetSuggestionsRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadSuggestionsStringCompleted), webRequest);
        }

        void client_DownloadSuggestionsStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                System.Net.HttpStatusCode status = httpResoponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    String jsonString = new StreamReader(httpResoponse.GetResponseStream()).ReadToEnd();
                    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                    {
                        //parse into jsonser
                        var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                        TraktMovie[] obj = (TraktMovie[])ser.ReadObject(ms);
                        int counter = 1;
                        this.SuggestItems = new ObservableCollection<ListItemViewModel>();

                        foreach (TraktMovie movie in obj)
                        {
                            if (counter++ > 8)
                                break;

                            this.SuggestItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, Type = "Movie", InWatchList = movie.InWatchlist, SubItemText = movie.year.ToString() });   
                        }

                    }
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException)
            { ErrorManager.ShowConnectionErrorPopup(); }

            LoadingSuggestItems = false;

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NotifyPropertyChanged("SuggestItems");
                NotifyPropertyChanged("LoadingStatus");
            });
        }

        #endregion

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
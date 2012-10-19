using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using WPtrakt.Model.Trakt;
using System.Threading;
using WPtrakt.Model;
using VPtrakt.Controllers;
using WPtrakt.Controllers;
using System.IO.IsolatedStorage;

namespace WPtrakt
{
    public class MyMoviesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> MovieItems { get; private set; }
        public ObservableCollection<ListItemViewModel> SuggestItems { get; private set; }
        private List<TraktMovie> suggestedMovies;
        private BackgroundWorker worker;
        public Boolean LoadingSuggestItems { get; set; }

        public MyMoviesViewModel()
        {
            this.MovieItems = new ObservableCollection<ListItemViewModel>();
            this.SuggestItems = new ObservableCollection<ListItemViewModel>();
            worker = new BackgroundWorker(); 
        }

        #region Getters/Setters

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public String LoadingStatusMovies
        {
            get
            {
                if (MovieItems.Count == 0)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public String LoadingStatusSuggestions
        {
            get
            {
                if (SuggestItems.Count == 0)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public bool SuggestionsEnabled
        {
            get
            {
                return AppUser.UserIsHighEndDevice();
            }
        }

        public String SuggestionsVisibility
        {
            get
            {
                if (SuggestionsEnabled)
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
            this.MovieItems = new ObservableCollection<ListItemViewModel>();
            RefreshMyMoviesView();
            String fileName ="mymovies.json";
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

        void myMoviesworker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
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

            request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/user/library/movies/all.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
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
        }

        private void UpdateMyMovieView(ObservableCollection<ListItemViewModel> tempItems)
        {

            this.MovieItems = tempItems;
            RefreshMyMoviesView();
        }

        private void RefreshMyMoviesView()
        {
            NotifyPropertyChanged("MovieItems");
            NotifyPropertyChanged("LoadingStatusMovies");
        }

        #endregion

        #region Suggest

        public void LoadSuggestData()
        {
            HttpWebRequest request;

            request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/recommendations/movies/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
            request.Method = "POST";
            request.BeginGetRequestStream(new AsyncCallback(GetSuggestionsRequestStreamCallback), request);

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
                        suggestedMovies = new List<TraktMovie>();
                        this.SuggestItems = new ObservableCollection<ListItemViewModel>();

                        foreach (TraktMovie movieInList in obj)
                        {
                            if (counter++ > 8)
                                break;

                            var movie = movieInList;
                            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                           {
                               this.SuggestItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, Type = "Movie" });
                               NotifyPropertyChanged("SuggestItems");
                           });

                            Thread.Sleep(1500);
                        }

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            NotifyPropertyChanged("LoadingStatusSuggestions");
                        });
                    }
                }

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        #endregion

        public void ClearItems()
        {
            this.MovieItems.Clear();
            this.IsDataLoaded = false;
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
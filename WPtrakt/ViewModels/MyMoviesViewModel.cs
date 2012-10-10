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

namespace WPtrakt
{
    public class MyMoviesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> MovieItems { get; private set; }
        public ObservableCollection<ListItemViewModel> SuggestItems { get; private set; }
        private List<TraktMovie> suggestedMovies;
        private BackgroundWorker worker;

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

        public void LoadData()
        {
            var trendingClient = new WebClient();
            trendingClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadMoviesStringCompleted);
            trendingClient.UploadStringAsync(new Uri("http://api.trakt.tv/user/library/movies/all.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());

            this.IsDataLoaded = true;
        }

        void client_UploadMoviesStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    //parse into jsonser
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                    TraktMovie[] obj = (TraktMovie[])ser.ReadObject(ms);

                    foreach (TraktMovie movie in obj)
                    {
                        tempItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, SubItemText = movie.year.ToString(), Genres = movie.Genres });
                    }

                    if (tempItems.Count == 0)
                    {
                        tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
                    }

                    this.MovieItems = tempItems;
                    NotifyPropertyChanged("MovieItems");
                    NotifyPropertyChanged("LoadingStatusMovies");
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        public void LoadSuggestData()
        {
            var suggestClient = new WebClient();
           
            suggestClient.UploadStringCompleted += new UploadStringCompletedEventHandler(suggestClient_UploadStringCompleted);
            suggestClient.UploadStringAsync(new Uri("http://api.trakt.tv/recommendations/movies/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication());
        }

        void suggestClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    //parse into jsonser
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                    TraktMovie[] obj = (TraktMovie[])ser.ReadObject(ms);
                    int counter = 1;
                    suggestedMovies = new List<TraktMovie>();
                    this.SuggestItems = new ObservableCollection<ListItemViewModel>();
                    foreach (TraktMovie movie in obj)
                    {
                        suggestedMovies.Add(movie);
                        NotifyPropertyChanged("SuggestItems");
                        if (counter++ >= 10)
                            break;
                    }

                    worker.WorkerReportsProgress = true;
                    worker.WorkerSupportsCancellation = true;
                    worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                    worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
                    worker.RunWorkerAsync();
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            NotifyPropertyChanged("LoadingStatusSuggestions");
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (suggestedMovies != null)
            {
              
                    foreach (TraktMovie movieInList in suggestedMovies)
                    {
                        var movie = movieInList;
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            this.SuggestItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id });
                            NotifyPropertyChanged("SuggestItems");
                        
                        });

                        if (StorageController.doesFileExist(movie.imdb_id + "medium" + ".jpg"))
                            Thread.Sleep(500);
                        else
                            Thread.Sleep(1000);
                    }
                  
                
            }
        }

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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using VPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Controllers;
using System.Windows.Media;
using System.Windows;


namespace WPtrakt
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> TrendingItems { get; private set; }
        public ObservableCollection<ListItemViewModel> HistoryItems { get; private set; }
        private TraktProfile _profile;
        private List<TraktMovie> trendingMovies;
      

        public MainViewModel()
        {
            this.TrendingItems = new ObservableCollection<ListItemViewModel>();
          
        }

        #region Getters/Setters

     

        public TraktProfile Profile
        {
            get
            {
               return _profile;
            }
            set
            {
                _profile = value;
                NotifyPropertyChanged("LoadingStatus");
                NotifyPropertyChanged("MainVisibility");
            }
        }

        public String UserAvatar
        {
            get
            {
                if (_profile != null)
                    return _profile.Avatar;
                else
                    return "";
            }
        }

        public String UserName
        {
            get
            {
                if (_profile != null)
                    return _profile.Username;
                else
                    return "";
            }
        }

        public String UserAbout
        {
            get
            {
                if (_profile != null)
                    return (!String.IsNullOrEmpty(_profile.Location) ? _profile.Location : "Omicron Persei 8") + ((!String.IsNullOrEmpty(_profile.About) ? ", " + _profile.About : ""));
                else
                    return "";
            }
        }

        public bool TrendingEnabled
        {
            get
            {
                return AppUser.UserIsHighEndDevice();
            }
        }

        public String TrendingVisibility
        {
            get
            {
                if (TrendingEnabled)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public String MainVisibility
        {
            get
            {
                if (_profile != null)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public String LoadingStatus
        {
            get
            {
                if (_profile == null)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        private String _loadingStatusTrending;
        public String LoadingStatusTrending
        {
            get
            {
                return _loadingStatusTrending;
            }
            set
            {
                _loadingStatusTrending = value;
                NotifyPropertyChanged("LoadingStatusTrending");
            }
        }

        public Boolean PanoramaEnabled
        {
            get
            {
                if (_profile == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        #endregion

        public void LoadData()
        {
            if (StorageController.doesFileExist(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json"))
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = false;
                worker.WorkerSupportsCancellation = false;
                worker.DoWork += new DoWorkEventHandler(profileworker_DoWork);
       
                worker.RunWorkerAsync();
            }
            else
            {
                CallProfileService();
            }
            if (TrendingEnabled)
            {
                this.LoadingStatusTrending = "Visible";
                var trendingClient = new WebClient();
                trendingClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_DownloadTrendingStringCompleted);
                trendingClient.UploadStringAsync(new Uri("http://api.trakt.tv/movies/trending.json/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication());
            }

            this.IsDataLoaded = true;
        }

        void profileworker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
            _profile = (TraktProfile)StorageController.LoadObject(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json", typeof(TraktProfile));
            if ((DateTime.Now - _profile.DownloadTime).Days < 1)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    loadHistory();
                    RefreshProfile();
                });
            }
            else
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                 CallProfileService();
                });
            }
        }


        private void CallProfileService()
        {
            var profileClient = new WebClient();

            profileClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_DownloadProfileStringCompleted);
            profileClient.UploadStringAsync(new Uri("http://api.trakt.tv/user/profile.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
        }

        void client_DownloadProfileStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktProfile));
                    _profile = (TraktProfile)ser.ReadObject(ms);
                    StorageController.saveObject(_profile, typeof(TraktProfile));
                    loadHistory();
                    RefreshProfile();
                }
            }
            catch (WebException)
            {
                _profile = new TraktProfile();
                NotifyPropertyChanged("MainVisibility");
                NotifyPropertyChanged("LoadingStatus");
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void RefreshProfile()
        {
            NotifyPropertyChanged("UserAvatar");
            NotifyPropertyChanged("UserName");
            NotifyPropertyChanged("UserAbout");
            NotifyPropertyChanged("LoadingStatus");
            NotifyPropertyChanged("MainVisibility");
            NotifyPropertyChanged("PanoramaEnabled");
        }

        void client_DownloadTrendingStringCompleted(object sender, UploadStringCompletedEventArgs e)
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

                    trendingMovies = new List<TraktMovie>();

                    foreach (TraktMovie movie in obj)
                    {
                        trendingMovies.Add(movie);

                        if (counter++ >= 10)
                            break;
                    }
                }

                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = false;
                worker.WorkerSupportsCancellation = false;
                worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
                worker.RunWorkerAsync();
            }
            catch (WebException)
            {
                //Do nothing yet
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.LoadingStatusTrending = "Collapsed";
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.TrendingItems = new ObservableCollection<ListItemViewModel>();
            if (trendingMovies != null)
            {
                    foreach (TraktMovie movieInList in trendingMovies)
                    {
                        var movie = movieInList;
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                           this.TrendingItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, Watched = movie.Watched, Rating = movie.Ratings.Percentage, NumberOfRatings = movie.Ratings.Votes.ToString() });
                           NotifyPropertyChanged("TrendingItems");

                        });

                        if(StorageController.doesFileExist(movie.imdb_id + "medium" + ".jpg"))
                            Thread.Sleep(250);
                        else
                            Thread.Sleep(500);
                    }
            }
        }

        private void loadHistory()
        {
            this.HistoryItems = new ObservableCollection<ListItemViewModel>();

            foreach (TraktWatched watched in _profile.Watched)
            {
                if (watched.Episode != null)
                    this.HistoryItems.Add(new ListItemViewModel() { Name = watched.Episode.Title, ImageSource = watched.Episode.Images.Screen, Imdb = watched.Show.imdb_id + watched.Episode.Season + watched.Episode.Number, SubItemText = "Season " + watched.Episode.Season + ", Episode " + watched.Episode.Number, Episode = watched.Episode.Number, Season = watched.Episode.Season, Tvdb = watched.Show.tvdb_id });
            }

            if (this.HistoryItems.Count == 0)
                this.HistoryItems.Add(new ListItemViewModel() { Name = "No recent history" });
                
            NotifyPropertyChanged("HistoryItems");
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
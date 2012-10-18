﻿using System;
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
using Microsoft.Phone.Shell;
using Coding4Fun.Phone.Controls;
using System.Windows.Threading;


namespace WPtrakt
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> TrendingItems { get; private set; }
        public ObservableCollection<ListItemViewModel> HistoryItems { get; private set; }
        private TraktProfile _profile;
        private DateTime firstCall { get; set; }
        public Boolean LoadingTrendingItems { get; set; }
      
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
            CallValidationService();
    

            this.IsDataLoaded = true;
        }

        #region Profile

        private void LoadProfile()
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

        }

        void profileworker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(300);
            _profile = (TraktProfile)StorageController.LoadObject(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json", typeof(TraktProfile));
            if ((DateTime.Now - _profile.DownloadTime).Days < 1)
            {
                loadHistory();

                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NotifyPropertyChanged("HistoryItems");
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

        DispatcherTimer tmr;

        private void CallValidationService()
        {
            var validationClient = new WebClient();
            firstCall = DateTime.Now;
            tmr = new DispatcherTimer();
            tmr.Interval = TimeSpan.FromSeconds(1);
            tmr.Tick += OnTimerTick;
            tmr.Start();

            validationClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_DownloadValidationStringCompleted);
            validationClient.UploadStringAsync(new Uri("http://api.trakt.tv/account/test/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
        }

        void OnTimerTick(object sender, EventArgs e)
        {
            int seconds = (DateTime.Now - firstCall).Seconds;

            if (seconds > 15)
            {
                var toast = new ToastPrompt
                {
                    Title = "Connection",
                    TextOrientation = System.Windows.Controls.Orientation.Vertical,
                    Message = "Connection to Trakt slow!",
                };
                toast.Show();

                tmr.Stop();
            }
        } 

        private void CallProfileService()
        {
            var profileClient = new WebClient();

            profileClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_DownloadProfileStringCompleted);
            profileClient.UploadStringAsync(new Uri("http://api.trakt.tv/user/profile.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
        }


        void client_DownloadValidationStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            tmr.Stop();
           

            try
            {
                String jsonString = e.Result;
                LoadProfile();
            }
            catch (WebException)
            {
                var toast = new ToastPrompt()
                {
                    Title = "User incorrect!",
                    TextOrientation = System.Windows.Controls.Orientation.Vertical,
 
                    Message = "Login data incorrect, or server connection problems.",
                };
                toast.Show();
                _profile = new TraktProfile();
                NotifyPropertyChanged("LoadingStatus");
            }
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
                    NotifyPropertyChanged("HistoryItems");
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
            NotifyPropertyChanged("HistoryItems");
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
        }

        #endregion

        #region Trending

        public void loadTrending()
        {
            if (TrendingEnabled)
            {
                this.LoadingStatusTrending = "Visible";
                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/movies/trending.json/5eaaacc7a64121f92b15acf5ab4d9a0b"));
                request.Method = "POST";
                request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);
            }
        }

        void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadTrendingStringCompleted), webRequest);
        }

        void client_DownloadTrendingStringCompleted(IAsyncResult r)
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
                        this.TrendingItems = new ObservableCollection<ListItemViewModel>();
                        int count = 0;
                        foreach (TraktMovie traktMovie in obj)
                        {
                            if (++count > 8)
                                break;

                            TraktMovie movie = traktMovie;
                            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                this.TrendingItems.Add(new ListItemViewModel() { Name = movie.Title, ImageSource = movie.Images.Poster, Imdb = movie.imdb_id, Watched = movie.Watched, Rating = movie.Ratings.Percentage, NumberOfRatings = movie.Ratings.Votes.ToString(), Type = "Movie" });
                                NotifyPropertyChanged("TrendingItems");
                            });
                            Thread.Sleep(1500);
                        }
                       
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            this.LoadingStatusTrending = "Collapsed";
                        });
                    }
                }
            }
            catch (WebException)
            {
                //Do nothing yet
            }
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
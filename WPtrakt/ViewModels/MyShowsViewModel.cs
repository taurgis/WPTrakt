using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using VPtrakt.Controllers;
using WPtrakt.Controllers;


namespace WPtrakt
{
    public class MyShowsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ShowItems { get; private set; }
        public ObservableCollection<CalendarListItemViewModel> CalendarItems { get; private set; }
        public ObservableCollection<ListItemViewModel> SuggestItems { get; private set; }
        private List<TraktShow> suggestedShows;
        private BackgroundWorker worker = new BackgroundWorker();

        public MyShowsViewModel()
        {
            this.ShowItems = new ObservableCollection<ListItemViewModel>();
            this.SuggestItems = new ObservableCollection<ListItemViewModel>();
            this.CalendarItems = new ObservableCollection<CalendarListItemViewModel>();
        }

        #region Getters/Setters

        public bool IsDataLoaded
        {
            get;
            private set;
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

        public String LoadingStatusShows
        {
            get
            {
                if (ShowItems.Count == 0)
                {
                    return "Visible";
                }
                else
                {
                    return "Collapsed";
                }
            }
        }

        public String LoadingStatusCalendar
        {
            get
            {
                if (CalendarItems.Count == 0)
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

        #endregion

        public void LoadData()
        {
            var trendingClient = new WebClient();
            trendingClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadShowsStringCompleted);
            trendingClient.UploadStringAsync(new Uri("http://api.trakt.tv/user/library/shows/all.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());

            this.IsDataLoaded = true;
        }

        void client_UploadShowsStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    //parse into jsonser
                    var ser = new DataContractJsonSerializer(typeof(TraktShow[]));
                    TraktShow[] obj = (TraktShow[])ser.ReadObject(ms);

                    foreach (TraktShow show in obj)
                    {
                        tempItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id, SubItemText = show.year.ToString(), Genres = show.Genres });
                    }

                    if (tempItems.Count == 0)
                    {
                        tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
                    }

                    this.ShowItems = tempItems;
                    NotifyPropertyChanged("ShowItems");
                    
                }
                NotifyPropertyChanged("LoadingStatusShows");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        public void LoadCalendarData()
        {
            var calendarClient = new WebClient();
            calendarClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadCalendartringCompleted);
            calendarClient.UploadStringAsync(new Uri("http://api.trakt.tv/user/calendar/shows.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());

            this.IsDataLoaded = true;
        }

        void client_UploadCalendartringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                ObservableCollection<CalendarListItemViewModel> tempItems = new ObservableCollection<CalendarListItemViewModel>();
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    //parse into jsonser
                    var ser = new DataContractJsonSerializer(typeof(TraktCalendar[]));
                    TraktCalendar[] obj = (TraktCalendar[])ser.ReadObject(ms);

                    foreach (TraktCalendar calendarDate in obj)
                    {
                        ObservableCollection<ListItemViewModel> tempEpisodes = new ObservableCollection<ListItemViewModel>();
                        foreach (TraktCalendarEpisode episode in calendarDate.Episodes)
                        {
                            tempEpisodes.Add(new ListItemViewModel() { Name = episode.Show.Title, ImageSource = episode.Episode.Images.Screen, Imdb = episode.Show.imdb_id + episode.Episode.Season + episode.Episode.Number, SubItemText = episode.Episode.Title, TruncateTitle = false, Tvdb = episode.Show.tvdb_id, Episode = episode.Episode.Number, Season = episode.Episode.Season });

                        }
                        tempItems.Add(new CalendarListItemViewModel() { Date = calendarDate.Date, Items = tempEpisodes });
                    }

                    if (obj.Length == 00)
                    {
                        tempItems.Add(new CalendarListItemViewModel() { Date = "No upcomming episodes", Items = new ObservableCollection<ListItemViewModel>() });
                    }

                    this.CalendarItems = tempItems;
                    NotifyPropertyChanged("LoadingStatusCalendar");
                    NotifyPropertyChanged("CalendarItems");
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
            suggestClient.UploadStringAsync(new Uri("http://api.trakt.tv/recommendations/shows/5eaaacc7a64121f92b15acf5ab4d9a0b"), AppUser.createJsonStringForAuthentication());
        }

        void suggestClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    //parse into jsonser
                    var ser = new DataContractJsonSerializer(typeof(TraktShow[]));
                    TraktShow[] obj = (TraktShow[])ser.ReadObject(ms);
                    int counter = 1;
                    suggestedShows = new List<TraktShow>();
                    this.SuggestItems = new ObservableCollection<ListItemViewModel>();
                    foreach (TraktShow show in obj)
                    {
                        suggestedShows.Add(show);
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
            if (suggestedShows != null)
            {
              
                    foreach (TraktShow showInList in suggestedShows)
                    {
                        var show = showInList;
                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            this.SuggestItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id });
                         
                            NotifyPropertyChanged("SuggestItems");
                        });

                        if (ImageController.doesImageFileExist(show.imdb_id + "medium" + ".jpg"))
                            Thread.Sleep(500);
                        else
                            Thread.Sleep(1000);
                    }
                    
                
            }
        }

        public void ClearItems()
        {
            this.ShowItems.Clear();
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
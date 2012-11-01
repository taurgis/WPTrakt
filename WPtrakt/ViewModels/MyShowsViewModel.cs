using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using VPtrakt.Controllers;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;


namespace WPtrakt
{
    public class MyShowsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ListItemViewModel> ShowItems { get; private set; }
        public ObservableCollection<CalendarListItemViewModel> CalendarItems { get; private set; }
        public ObservableCollection<ListItemViewModel> SuggestItems { get; private set; }
        private Boolean LoadingSuggestItems { get; set; }
        private Boolean LoadingCalendar { get; set; }
        private Boolean LoadingMyShows { get; set; }

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

        #region MyShows

        public void LoadData()
        {
            if (!this.LoadingMyShows)
            {
                this.LoadingMyShows = true;
                this.ShowItems = new ObservableCollection<ListItemViewModel>();
                RefreshMyShowsView();
                String fileName = "myshows.json";
                if (StorageController.doesFileExist(fileName))
                {
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = false;
                    worker.WorkerSupportsCancellation = false;
                    worker.DoWork += new DoWorkEventHandler(myShowsworker_DoWork);

                    worker.RunWorkerAsync();
                }
                else
                {
                    CallMyShowService();
                }

                this.IsDataLoaded = true;
            }
        }

        void myShowsworker_DoWork(object sender, DoWorkEventArgs e)
        {
            String fileName = "myshows.json";
            if ((DateTime.Now - IsolatedStorageFile.GetUserStoreForApplication().GetLastWriteTime(fileName)).Days < 1)
            {

                TraktShow[] myShows = (TraktShow[])StorageController.LoadObjectFromMain(fileName, typeof(TraktShow[]));

                ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();
                foreach (TraktShow show in myShows)
                {
                    tempItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id, SubItemText = show.year.ToString(), Genres = show.Genres });
                }

                if (tempItems.Count == 0)
                {
                    tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
                }

                this.LoadingMyShows = false;
              
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.ShowItems = tempItems;
                    RefreshMyShowsView();
                });
            }
            else
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    CallMyShowService();
                });
            }
        }

        private void CallMyShowService()
        {
            HttpWebRequest request;

            request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/user/library/shows/all.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
            request.Method = "POST";
            request.BeginGetRequestStream(new AsyncCallback(GetMyShowsRequestStreamCallback), request);
        }

        void GetMyShowsRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadMyShowsStringCompleted), webRequest);
        }

        void client_DownloadMyShowsStringCompleted(IAsyncResult r)
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
                        var ser = new DataContractJsonSerializer(typeof(TraktShow[]));
                        TraktShow[] obj = (TraktShow[])ser.ReadObject(ms);
                        StorageController.saveObjectInMainFolder(obj, typeof(TraktShow[]), "myshows.json");
                        foreach (TraktShow show in obj)
                        {
                            tempItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id, SubItemText = show.year.ToString(), Genres = show.Genres });
                        }

                        if (tempItems.Count == 0)
                        {
                            tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
                        }

                        this.LoadingMyShows = false;
                        this.ShowItems = tempItems;

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            NotifyPropertyChanged("ShowItems");
                            NotifyPropertyChanged("LoadingStatusShows");
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

        #region Calendar

        public void LoadCalendarData()
        {
            if (!this.LoadingCalendar)
            {
                this.LoadingCalendar = true;
                this.CalendarItems = new ObservableCollection<CalendarListItemViewModel>();
                NotifyPropertyChanged("CalendarItems");
                NotifyPropertyChanged("LoadingStatusCalendar");
                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/user/calendar/shows.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName + "/" + DateTime.Now.ToString("yyyyMMdd") + "/14"));
                request.Method = "POST";
                request.BeginGetRequestStream(new AsyncCallback(GetCalendarRequestStreamCallback), request);
            }
        }

        void GetCalendarRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadCalendarStringCompleted), webRequest);
        }

        void client_DownloadCalendarStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                System.Net.HttpStatusCode status = httpResoponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    String jsonString = new StreamReader(httpResoponse.GetResponseStream()).ReadToEnd();
                    ObservableCollection<CalendarListItemViewModel> tempItems = new ObservableCollection<CalendarListItemViewModel>();
                    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                    {
                        //parse into jsonser
                        var ser = new DataContractJsonSerializer(typeof(TraktCalendar[]));
                        TraktCalendar[] obj = (TraktCalendar[])ser.ReadObject(ms);
                        StorageController.saveObjectInMainFolder(obj, typeof(TraktCalendar[]), "upcomming.json");

                        foreach (TraktCalendar calendarDate in obj)
                        {
                            if ((DateTime.Parse(calendarDate.Date) - DateTime.Now).Days > 7)
                                break;
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
                        this.LoadingCalendar = false;

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            NotifyPropertyChanged("LoadingStatusCalendar");
                            NotifyPropertyChanged("CalendarItems");
                        });
                    }

                }

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }

        private void RefreshMyShowsView()
        {
            NotifyPropertyChanged("ShowItems");
            NotifyPropertyChanged("LoadingStatusShows");
        }

        #endregion

        #region Suggestions

        public void LoadSuggestData()
        {
            if (!this.LoadingSuggestItems)
            {
                this.LoadingSuggestItems = true;
                this.SuggestItems = new ObservableCollection<ListItemViewModel>();
                NotifyPropertyChanged("SuggestItems");
                NotifyPropertyChanged("LoadingStatusSuggestions");

                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri("http://api.trakt.tv/recommendations/shows/9294cac7c27a4b97d3819690800aa2fedf0959fa"));
                request.Method = "POST";
                request.BeginGetRequestStream(new AsyncCallback(GetSuggestRequestStreamCallback), request);
            }
        }

        void GetSuggestRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadSuggestStringCompleted), webRequest);
        }

        void client_DownloadSuggestStringCompleted(IAsyncResult r)
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
                        var ser = new DataContractJsonSerializer(typeof(TraktShow[]));
                        TraktShow[] obj = (TraktShow[])ser.ReadObject(ms);
                        int counter = 1;
                        this.SuggestItems = new ObservableCollection<ListItemViewModel>();

                        foreach (TraktShow show in obj)
                        {
                            if (counter++ > 8)
                                break;

                            this.SuggestItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id, Type = "Show", InWatchList = show.InWatchlist });
                        }

                        LoadingSuggestItems = false;

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            NotifyPropertyChanged("SuggestItems");
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

        public void ClearItems()
        {
            this.ShowItems.Clear();
            this.IsDataLoaded = false;
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
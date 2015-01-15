using Microsoft.Phone.Shell;
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
using System.Threading;
using WPtrakt.Controllers;
using WPtrakt.Custom;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controller;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;


namespace WPtrakt
{
    public class MyShowsViewModel : INotifyPropertyChanged
    {
        public ProgressIndicator Indicator { get; set; }
        public ObservableCollection<AlphaKeyGroup<ListItemViewModel>> ShowItems { get; private set; }
        public ObservableCollection<ListItemViewModel> SuggestItems { get; private set; }
        private Boolean LoadingSuggestItems { get; set; }
   
        private Boolean LoadingMyShows { get; set; }
        private ShowController controller = new ShowController();

        public MyShowsViewModel()
        {
            this.ShowItems = new ObservableCollection<AlphaKeyGroup<ListItemViewModel>>();
            this.SuggestItems = new ObservableCollection<ListItemViewModel>();
        }

        #region Getters/Setters

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        #endregion

        #region MyShows
        int filter = 0;
        public void LoadData(int filter)
        {
            if (!this.LoadingMyShows)
            {
                this.filter = filter;
                this.LoadingMyShows = true;
                this.ShowItems = new ObservableCollection<AlphaKeyGroup<ListItemViewModel>>();
                App.MyShowsViewModel.NotifyPropertyChanged("ShowItems");
                LoadShows();
            }
        }
        public TraktShowProgress[] fullprogress { get; set; }
        private async void LoadShows()
        {
            TraktShow[] myShows = await controller.getUserShows();
            List<String> seenShows = new List<string>();
            Dictionary<String, TraktShowProgress> progressDictionary = new Dictionary<string, TraktShowProgress>(); 
            String tvdbidstrings = "";
            foreach (TraktShow show in myShows)
            {
                tvdbidstrings += show.tvdb_id + ",";
            }

            if(fullprogress == null)
            {
                fullprogress = await controller.getShowProgressionByTVDBID(tvdbidstrings);
            }
               
            if(fullprogress != null)
            { 
                foreach (TraktShowProgress progress in fullprogress)
                {
                    if (!progressDictionary.ContainsKey(progress.Show.tvdb_id))
                    {
                        progressDictionary.Add(progress.Show.tvdb_id, progress);
                    }

                    if (progress.Progress.Percentage == 100)
                        seenShows.Add(progress.Show.tvdb_id);
                }
            }


            ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();
            foreach (TraktShow show in myShows)
            {
                if (filter > 0)
                {
                    if (filter == 1)
                    {
                        if (!seenShows.Contains(show.tvdb_id))
                            continue;
                    }
                    else if (filter == 2)
                    {
                        if (seenShows.Contains(show.tvdb_id))
                            continue;
                    }
                }
              
                        
                tempItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id, SubItemText = show.year.ToString(), Genres = show.Genres, Progress = progressDictionary.ContainsKey(show.tvdb_id) ? progressDictionary[show.tvdb_id].Progress.Percentage : Int16.Parse("0"), ProgressText =progressDictionary.ContainsKey(show.tvdb_id) ? progressDictionary[show.tvdb_id].Progress.Left + " episodes left to see!" : "Can't fetch unseen episodes :-("});
            }

            if (tempItems.Count == 0)
            {
                tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
            }

            this.LoadingMyShows = false;
            this.IsDataLoaded = true;
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {

                this.ShowItems = AlphaKeyGroup<ListItemViewModel>.CreateGroups(tempItems, Thread.CurrentThread.CurrentUICulture, (ListItemViewModel s) => { return s.Name; }, true);
                App.MyShowsViewModel.NotifyPropertyChanged("ShowItems");
                
                if(this.Indicator != null)
                    this.Indicator.IsVisible = false;
            });
        }

        public void FilterShows(int type)
        {
            if (type == 0)
            {
                this.LoadData(0);
            }
            else if (type == 1)
            {
                this.LoadMyWatchListShowsData();
            }
            else if (type == 2)
            {
                this.LoadData(1);
            }
            else if (type == 3)
            {
                this.LoadData(2);
            }
        }
        #endregion

        #region MyWatchListShows

        public void LoadMyWatchListShowsData()
        {
            if (!this.LoadingMyShows)
            {
                this.LoadingMyShows = true;
                this.ShowItems = new ObservableCollection<AlphaKeyGroup<ListItemViewModel>>();
                App.MyShowsViewModel.NotifyPropertyChanged("ShowItems");

                LoadWatchlist();
                

            }
        }

        private async void LoadWatchlist()
        {

            TraktShow[] myShows = await controller.getUserWatchlist();
            List<String> seenShows = new List<string>();
            Dictionary<String, TraktShowProgress> progressDictionary = new Dictionary<string, TraktShowProgress>();

            if (fullprogress != null)
            {
                foreach (TraktShowProgress progress in fullprogress)
                {
                    if (!progressDictionary.ContainsKey(progress.Show.tvdb_id))
                    {
                        progressDictionary.Add(progress.Show.tvdb_id, progress);
                    }

                    if (progress.Progress.Percentage == 100)
                        seenShows.Add(progress.Show.tvdb_id);
                }
            }

            ObservableCollection<ListItemViewModel> tempItems = new ObservableCollection<ListItemViewModel>();
            foreach (TraktShow show in myShows)
            {
                tempItems.Add(new ListItemViewModel() { Name = show.Title, ImageSource = show.Images.Poster, Imdb = show.imdb_id, Tvdb = show.tvdb_id, SubItemText = show.year.ToString(), Genres = show.Genres, Progress = progressDictionary.ContainsKey(show.tvdb_id) ? progressDictionary[show.tvdb_id].Progress.Percentage : Int16.Parse("0"), ProgressText = progressDictionary.ContainsKey(show.tvdb_id) ? progressDictionary[show.tvdb_id].Progress.Left + " episodes left to see!" : "Can't fetch unseen episodes :-(" });

            }

            if (tempItems.Count == 0)
            {
                tempItems.Add(new ListItemViewModel() { Name = "Nothing Found" });
            }

            this.LoadingMyShows = false;

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.ShowItems = AlphaKeyGroup<ListItemViewModel>.CreateGroups(tempItems, Thread.CurrentThread.CurrentUICulture, (ListItemViewModel s) => { return s.Name; }, true);
                App.MyShowsViewModel.NotifyPropertyChanged("ShowItems");

                if (this.Indicator != null)
                    this.Indicator.IsVisible = false;
            });

        }



        #endregion

        #region Suggestions


        public async void LoadSuggestData()
        {
            LoadingSuggestItems = true;
            TraktShow[] obj = await controller.getUserSuggestions();
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
                if (this.Indicator != null)
                    this.Indicator.IsVisible = false;
                NotifyPropertyChanged("SuggestItems");
                NotifyPropertyChanged("LoadingStatus");
            });
        }

        public void ClearItems()
        {
            this.ShowItems.Clear();
            this.IsDataLoaded = false;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
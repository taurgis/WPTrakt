﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtraktBase.Controller;
using WPtraktBase.Model.Trakt;

namespace WPtrakt
{
    public class ShowViewModel : INotifyPropertyChanged
    {

        public ObservableCollection<ListItemViewModel> ShoutItems { get; private set; }
        public ObservableCollection<ListItemViewModel> EpisodeItems { get; set; }
        public ObservableCollection<ListItemViewModel> UnWatchedEpisodeItems { get; set; }

        public ObservableCollection<ListItemViewModel> RelatedShows { get; set; }
        private Int16 numberOfSeasons { get; set; }
        public Int16 currentSeason { get; set; }
        public Boolean ShoutsLoaded { get; set; }

        public ShowViewModel()
        {
            this.ShoutsLoaded = false;
            this.ShoutItems = new ObservableCollection<ListItemViewModel>();
            this.ShoutItems.Add(new ListItemViewModel() { Name = "Loading..." });
            this.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            this.RelatedShows = new ObservableCollection<ListItemViewModel>();
        }

        public void clearShouts()
        {
            this.ShoutItems = new ObservableCollection<ListItemViewModel>();
            NotifyPropertyChanged("ShoutItems");
        }

        public void clearRelatedShows()
        {
            this.RelatedShows = new ObservableCollection<ListItemViewModel>();
            NotifyPropertyChanged("RelatedShows");
        }

        public void addShout(ListItemViewModel model)
        {
            if (this.ShoutItems == null)
                this.ShoutItems = new ObservableCollection<ListItemViewModel>();

            this.ShoutItems.Add(model);
            NotifyPropertyChanged("ShoutItems");
        }

        public void updateEpisode(TraktEpisode traktEpisode)
        {
            foreach (ListItemViewModel model in EpisodeItems)
            {
                if (!String.IsNullOrEmpty(model.Season))
                {
                    if (model.Season.Equals(traktEpisode.Season) && model.Episode.Equals(traktEpisode.Number))
                    {
                        model.Watched = traktEpisode.Watched;
                        model.InWatchList = traktEpisode.InWatchlist;
                        model.Rating = traktEpisode.MyRatingAdvanced;
                    }
                }
            }

            if (UnWatchedEpisodeItems != null)
            {
                foreach (ListItemViewModel subModel in UnWatchedEpisodeItems)
                {
                    if (!String.IsNullOrEmpty(subModel.Season))
                    {
                        if (subModel.Season.Equals(traktEpisode.Season) && subModel.Episode.Equals(traktEpisode.Number))
                        {
                            if (traktEpisode.Watched)
                            {
                                UnWatchedEpisodeItems.Remove(subModel);
                                break;
                            }
                        }
                    }
                }

            }
        }

        #region Getters/Setters

        public Int16 NumberOfSeasons
        {
            get
            {
                return this.numberOfSeasons;
            }
            set
            {
                this.numberOfSeasons = value;
                NotifyPropertyChanged("SeasonItems");
            }
        }

        public List<String> SeasonItems
        {
            get
            {
                if (this.numberOfSeasons > 0)
                {
                    List<String> seasons = new List<string>();
                    for (int i = 1; i <= this.numberOfSeasons; i++)
                    {
                        seasons.Add(i.ToString());
                    }

                    return seasons;
                }
                else
                    return new List<string>();
            }

        }


        private string _name;
        public string Name
        {
            get
            {
                if (String.IsNullOrEmpty(_name))
                    return "Loading..";
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private string _imdb;
        public string Imdb
        {
            get
            {
                return _imdb;
            }
            set
            {
                if (value != _imdb)
                {
                    _imdb = value;
                    NotifyPropertyChanged("Imdb");
                }
            }
        }

        private Boolean _InWatchlist;
        public Boolean InWatchlist
        {
            get
            {
                return _InWatchlist;
            }
            set
            {
                if (value != _InWatchlist)
                {
                    _InWatchlist = value;

                }
            }
        }

        private string _tvdb;
        public string Tvdb
        {
            get
            {
                return _tvdb;
            }
            set
            {
                if (value != _tvdb)
                {
                    _tvdb = value;
                    NotifyPropertyChanged("Tvdb");
                }
            }
        }

        private string _year;
        public string Year
        {
            get
            {
                return _year;
            }
            set
            {
                if (value != _year)
                {
                    _year = value;
                    NotifyPropertyChanged("Year");
                }
            }
        }

        private string _runtime;
        public string Runtime
        {
            get
            {
                return _runtime + " mins";
            }
            set
            {
                if (value != _runtime)
                {
                    _runtime = value;
                    NotifyPropertyChanged("Runtime");
                }
            }
        }

        private string _certification;
        public string Certification
        {
            get
            {
                return _certification;
            }
            set
            {
                if (value != _certification)
                {
                    _certification = value;
                    NotifyPropertyChanged("Certification");
                }
            }
        }

        private string[] _genres;
        public string[] Genres
        {
            get
            {
                return _genres;
            }
            set
            {
                if (value != _genres)
                {
                    _genres = value;
                    NotifyPropertyChanged("Genres");
                }
            }
        }

        public String GenreString
        {
            get
            {
                if (_genres == null)
                    return "";
                String genreString = "";
                foreach (String genre in _genres)
                {
                    genreString += genre + ", ";
                }
                if (!String.IsNullOrEmpty(genreString))
                    genreString = genreString.Remove(genreString.Length - 2, 2);
                return genreString;
            }
        }


        private string _overview;
        public string Overview
        {
            get
            {
                return _overview;
            }
            set
            {
                if (value != _overview)
                {
                    _overview = value;
                    NotifyPropertyChanged("Overview");
                }
            }
        }

        private string _fanart;
        public string Fanart
        {
            get
            {
                return _fanart;
            }
            set
            {
                if (value != _fanart)
                {
                    _fanart = value;
                    NotifyPropertyChanged("Fanart");
                }
            }
        }

        private BitmapImage _backgroundImage;
        public BitmapImage BackgroundImage
        {
            get
            {
                return _backgroundImage;
            }
            set
            {
                _backgroundImage = value;

                NotifyPropertyChanged("BackgroundImage");
            }
        }
        public String WallPaperImage { get; set; }

        private BitmapImage _wallPaper;
        public BitmapImage WallPaper
        {
            get
            {
                if (String.IsNullOrEmpty(WallPaperImage))
                    return null;

                if (_wallPaper == null)
                {
                    LoadWallpaparImage();

                    return null;
                }
                else
                {
                    return _wallPaper;
                }
            }

            set
            {
                _wallPaper = value;

                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged("WallPaper");
                }));
            }
        }

        private async void LoadWallpaparImage()
        {
            this.WallPaper = await ShowController.getLargeCoverImage(this.Imdb, this.WallPaperImage);
        }


        public String DetailVisibility
        {
            get
            {
                if (_name == null)
                {
                    return "Collapsed";
                }
                else
                {
                    return "Visible";
                }
            }
        }

        private Int16 _rating;
        public Int16 Rating
        {
            get
            {
                return _rating;
            }
            set
            {
                if (value != _rating)
                {
                    _rating = value;
                }
            }
        }

        private Boolean _watched;
        public Boolean Watched
        {
            get
            {
                return _watched;
            }
            set
            {
                if (value != _watched)
                {
                    _watched = value;
                }
            }
        }

        private Int16 _votes;
        public Int16 Votes
        {
            get
            {
                return _votes;
            }
            set
            {
                if (value != _votes)
                {
                    _votes = value;
                }
            }
        }
        public String VotesString
        {
            get
            {
                return this.Votes + " votes";
            }
        }

        public String RatingString
        {
            get
            {
                String baseString;
                baseString = this.Rating + "%";

                if (this.MyRating == null)
                    return "N/A";

                return baseString;
            }
        }

        public Uri AllRatingImage
        {
            get
            {
                if (this.Rating < 60)
                    return new Uri("Images/icon-hate-large.png", UriKind.Relative);
                else
                    return new Uri("Images/icon-love-large.png", UriKind.Relative);
            }
        }


        public Uri RatingImage
        {
            get
            {
                if (!this.MyRating.Equals("false"))
                    return new Uri("Images/badge-" + this.MyRatingAdvanced + ".png", UriKind.Relative);
                else
                    return null;
            }

        }

        private Int16 _myRatingAdvanced;
        public Int16 MyRatingAdvanced
        {
            get
            {
                return _myRatingAdvanced;
            }
            set
            {
                if (value != _myRatingAdvanced)
                {
                    _myRatingAdvanced = value;
                    NotifyPropertyChanged("MyRatingAdvanced");
                    NotifyPropertyChanged("RatingString");
                    NotifyPropertyChanged("RatingImage");
                }
            }
        }

        private String _myRating;
        public String MyRating
        {
            get
            {
                return _myRating;
            }
            set
            {
                if (value != _myRating)
                {
                    _myRating = value;
                }
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        #endregion

        public void LoadData(String tvdb)
        {
            if (currentSeason == 0)
                currentSeason = 1;

            this._tvdb = tvdb;
        }

        public void RefreshAll()
        {
            App.ShowViewModel.EpisodeItems = new ObservableCollection<ListItemViewModel>();
            App.ShowViewModel.UnWatchedEpisodeItems = null;

            NotifyPropertyChanged("DetailVisibility");
           
            clearShouts();
            RefreshEpisodes();
        }

        public void UpdateShowView(TraktShow show)
        {
            _name = show.Title;

            if (show.Images != null)
                _fanart = show.Images.Fanart;
            _genres = show.Genres;
            _imdb = show.imdb_id;
            _overview = show.Overview;
            WallPaperImage = show.Images.Poster;
            _runtime = show.Runtime.ToString();
            _certification = show.Certification;
            _year = show.year.ToString();
            _InWatchlist = show.InWatchlist;
            if (show.Ratings != null)
            {
                _rating = show.Ratings.Percentage;
                _votes = show.Ratings.Votes;
            }
            _myRating = show.MyRating;
            _myRatingAdvanced = show.MyRatingAdvanced;
            _watched = show.Watched;
            _tvdb = show.tvdb_id;
            _imdb = show.imdb_id;

            NotifyPropertyChanged("Name");
            NotifyPropertyChanged("Fanart");
            NotifyPropertyChanged("GenreString");
            NotifyPropertyChanged("Overview");
            NotifyPropertyChanged("Certification");
            NotifyPropertyChanged("Year");
            NotifyPropertyChanged("Runtime");
            NotifyPropertyChanged("DetailVisibility");
            NotifyPropertyChanged("RatingString");
            NotifyPropertyChanged("WallPaper");
        }


        public void RefreshEpisodes()
        {
            NotifyPropertyChanged("EpisodeItems");
        }


        public void RefreshUnwatchedEpisodes()
        {
            NotifyPropertyChanged("UnWatchedEpisodeItems");
        }

        public void RefreshRelatedShows()
        {
            NotifyPropertyChanged("RelatedShows");
        }
     

        private void CallEpisodesService(String tvdb)
        {
            var showClient = new WebClient();
            this._tvdb = tvdb;
            showClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadEpisodeStringCompleted);
            showClient.UploadStringAsync(new Uri("https://api.trakt.tv/show/season.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb + "/" + currentSeason), AppUser.createJsonStringForAuthentication());
        }

        void client_UploadEpisodeStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktEpisode[]));
                    TraktEpisode[] episodes = (TraktEpisode[])ser.ReadObject(ms);

                    foreach (TraktEpisode episode in episodes)
                    {
                        episode.Tvdb = this.Tvdb;
                        this.EpisodeItems.Add(new ListItemViewModel() { Name = episode.Title, ImageSource = episode.Images.Screen, Imdb = _imdb + episode.Season + episode.Number, SubItemText = "Season " + episode.Season + ", Episode " + episode.Number, Episode = episode.Number, Season = episode.Season, Tvdb = _tvdb, Watched = episode.Watched, Rating = episode.MyRatingAdvanced, InWatchList = episode.InWatchlist });
                    }
                    episodes[0].DownloadTime = DateTime.Now;

                    DateTime airTime = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
                    airTime = airTime.AddSeconds(episodes[episodes.Length-1].FirstAired);

                   int daysSinceRelease = (DateTime.Now - airTime).Days;

                    this.EpisodeItems.Add(new ListItemViewModel());


                    /*
                    if (!String.IsNullOrEmpty(this.Tvdb) && !(daysSinceRelease < 30))
                        StorageController.saveObject(episodes, typeof(TraktEpisode[]));
                    */
                }
                NotifyPropertyChanged("EpisodeItems");
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException)
            { ErrorManager.ShowConnectionErrorPopup(); }
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
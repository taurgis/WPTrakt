using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using WPtrakt.Model;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.DAO
{
    public class ShowDao : Dao
    {
        private static ShowDao _Instance { get; set; }
        public static ShowDao Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new ShowDao();

                return _Instance;
            }
        }

        private List<String> cachedShowUUIDS;
        private Boolean showAvailableInDatabaseByTVDB(String TVDB)
        {
            if (cachedShowUUIDS == null)
                cachedShowUUIDS = new List<string>();

            if (cachedShowUUIDS.Contains(TVDB))
                return true;

            try
            {
                if (!this.DatabaseExists())
                    return false;

                if (this.Shows.Count() == 0)
                    return false;

                Boolean exists = this.Shows.Where(t => t.tvdb_id == TVDB).Count() > 0;

                if (exists)
                {
                    cachedShowUUIDS.Add(TVDB);
                }

                return exists;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TraktShow> getShowByTVDB(String TVDB)
        {
            if (showAvailableInDatabaseByTVDB(TVDB))
            {
                TraktSeason season = this.Seasons.Where(t => t.Tvdb == TVDB).FirstOrDefault();
                return this.Shows.Where(t => t.tvdb_id == TVDB).FirstOrDefault();
            }
            else
                return await getShowByTVDBThroughTrakt(TVDB);
        }

        public TraktShow getShowByIMDB(String IMDB)
        {
       
            return this.Shows.Where(t => t.imdb_id == IMDB).FirstOrDefault();
        }

        private async Task<TraktShow> getShowByTVDBThroughTrakt(String TVDB)
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDB), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShow));
                    TraktShow show = (TraktShow)ser.ReadObject(ms);
                    show.DownloadTime = DateTime.Now;

                    foreach (String genre in show.Genres)
                        show.GenresAsString += genre + "|";

                    show.GenresAsString = show.GenresAsString.Remove(show.GenresAsString.Length - 1);
                    show.Seasons = new EntitySet<TraktSeason>();
                    
                    saveShow(show);

                    return show;
                }
            }
            catch (WebException)
            { }
            catch (TargetInvocationException)
            { }

            return null;
        }

        public Boolean saveSeasons(TraktSeason[] seasons, String tvdbid)
        {
            foreach (TraktSeason season in seasons)
            {
                season.Tvdb = tvdbid;
                this.Seasons.InsertOnSubmit(season);
            }
            this.SubmitChanges();
            return true;
        }

        public Boolean saveShow(TraktShow traktShow)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            if (showAvailableInDatabaseByTVDB(traktShow.tvdb_id))
            {
                updateShow(traktShow);
            }
            else
            {
                this.Shows.InsertOnSubmit(traktShow);
            }

            this.SubmitChanges(ConflictMode.FailOnFirstConflict);

            return true;
        }

        private async void updateShow(TraktShow traktShow)
        {
            TraktShow dbShow = await getShowByTVDB(traktShow.tvdb_id);
            dbShow.Certification = traktShow.Certification;
            dbShow.DownloadTime = traktShow.DownloadTime;
            dbShow.Genres = traktShow.Genres;
            dbShow.GenresAsString = traktShow.GenresAsString;
            dbShow.Images = traktShow.Images;
            dbShow.imdb_id = traktShow.imdb_id;
            dbShow.InWatchlist = traktShow.InWatchlist;
            dbShow.MyRating = traktShow.MyRating;
            dbShow.MyRatingAdvanced = traktShow.MyRatingAdvanced;
            dbShow.Overview = traktShow.Overview;
            dbShow.Ratings = traktShow.Ratings;
            dbShow.Runtime = traktShow.Runtime;
            dbShow.Title = traktShow.Title;
            dbShow.tvdb_id = traktShow.tvdb_id;
            dbShow.Url = traktShow.Url;
            dbShow.Watched = traktShow.Watched;
            dbShow.Watchers = traktShow.Watchers;
            dbShow.year = traktShow.year;
            dbShow.Seasons = traktShow.Seasons;
        }

        public async Task<TraktSeason[]> getSeasonsForTvShowByTVDBID(String TVDBID)
        {
            WebClient seasonClient = new WebClient();
            String jsonString = await seasonClient.DownloadStringTaskAsync(new Uri("http://api.trakt.tv/show/seasons.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDBID));
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktSeason[]));
                return (TraktSeason[])ser.ReadObject(ms);
            }
        }


        private Boolean episodeAvailableInDatabaseByTVDBAndSeasonInfo(String TVDB, String season, String episode)
        {
            try
            {
                if (!this.DatabaseExists())
                    return false;

                if (this.Episodes.Count() == 0)
                    return false;

                Boolean exists = this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Season == season) && (t.Number == episode)).Count() > 0;

                return exists;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TraktEpisode> getEpisodeByTvdbAndSeasonInfo(String TVDB, String season, String episode)
        {
            if (episodeAvailableInDatabaseByTVDBAndSeasonInfo(TVDB, season, episode))
            {
                return this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Season == season) && (t.Number == episode)).FirstOrDefault();
            }
            else
                return await getEpisodeByTVDBThroughTrakt(TVDB, season, episode);
        }

        private async Task<TraktEpisode> getEpisodeByTVDBThroughTrakt(String TVDB, String season, String episode)
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/episode/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDB + "/" + season + "/" + episode), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktWatched));
                    TraktEpisode tEpisode = ((TraktWatched)ser.ReadObject(ms)).Episode;
                    tEpisode.DownloadTime = DateTime.Now;
                    tEpisode.SeasonID = 0;
                    this.saveEpisode(tEpisode);
                    return tEpisode;
                }
            }
            catch (WebException)
            { }
            catch (TargetInvocationException)
            { }

            return null;
        }



        public Boolean saveEpisode(TraktEpisode traktEpisode)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            if (episodeAvailableInDatabaseByTVDBAndSeasonInfo(traktEpisode.Tvdb, traktEpisode.Season, traktEpisode.Number))
            {
                updateEpisode(traktEpisode);
            }
            else
            {
                this.Episodes.InsertOnSubmit(traktEpisode);
            }

            this.SubmitChanges(ConflictMode.FailOnFirstConflict);

            return true;
        }

        private async void updateEpisode(TraktEpisode traktEpisode)
        {
            TraktEpisode dbEpisode = await getEpisodeByTVDBThroughTrakt(traktEpisode.Tvdb, traktEpisode.Season, traktEpisode.Number);

            dbEpisode.DownloadTime = traktEpisode.DownloadTime;
            dbEpisode.EpisodeID = traktEpisode.EpisodeID;
            dbEpisode.FirstAired = traktEpisode.FirstAired;
            dbEpisode.Images = traktEpisode.Images;
            dbEpisode.InWatchlist = traktEpisode.InWatchlist;
            dbEpisode.MyRating = traktEpisode.MyRating;
            dbEpisode.MyRatingAdvanced = traktEpisode.MyRatingAdvanced;
            dbEpisode.Number = traktEpisode.Number;
            dbEpisode.Overview = traktEpisode.Overview;
            dbEpisode.Ratings = traktEpisode.Ratings;
            dbEpisode.Season = traktEpisode.Season;
            dbEpisode.SeasonID = traktEpisode.SeasonID;
            dbEpisode.Title = traktEpisode.Title;
            dbEpisode.Tvdb = traktEpisode.Tvdb;
            dbEpisode.Watched = traktEpisode.Watched;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
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
        internal static ShowDao Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new ShowDao();

                return _Instance;
            }
        }


        internal async Task<TraktShow> getShowByTVDB(String TVDB)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            if (this.Shows.Where(t => t.tvdb_id == TVDB).Count() > 0)
            {
                TraktShow show = this.Shows.Where(t => t.tvdb_id == TVDB).FirstOrDefault();

                Console.WriteLine("Show " + show.Title + " fetched from DB.");
                return show;
            }
            else
                return await getShowByTVDBThroughTrakt(TVDB);
        }

        internal TraktShow getShowByIMDB(String IMDB)
        {
            TraktShow show = this.Shows.Where(t => t.imdb_id == IMDB).FirstOrDefault();

           Console.WriteLine("Show " + show.Title + " fetched from DB.");
            return show;
        }

        internal async void deleteShowByTvdbId(String TVDBid)
        {
            TraktShow show = await getShowByTVDB(TVDBid);
            foreach (TraktSeason season in show.Seasons)
                this.Seasons.DeleteOnSubmit(season);
             this.Shows.DeleteOnSubmit(show);
             this.SubmitChanges(ConflictMode.FailOnFirstConflict);
        }

        private async Task<TraktShow> getShowByTVDBThroughTrakt(String TVDB)
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDB), AppUser.createJsonStringForAuthentication());
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
                   Console.WriteLine("Show " + show.Title + " fetched from Trakt Server.");
                    return show;
                }
            }
            catch (WebException)
            { }
            catch (TargetInvocationException)
            { }

            return null;
        }

        internal Boolean saveSeasons(TraktSeason[] seasons, String tvdbid)
        {
            foreach (TraktSeason season in seasons)
            {
                season.Tvdb = tvdbid;
                this.Seasons.InsertOnSubmit(season);
            }
            this.SubmitChanges();
            return true;
        }

        internal Boolean saveShow(TraktShow traktShow)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            if (this.Shows.Where(t => t.tvdb_id == traktShow.tvdb_id).Count() > 0)
            {
               updateShow(traktShow);
               Console.WriteLine("Show " + traktShow.Title + " updated to DB.");
            }
            else
            {
                this.Shows.InsertOnSubmit(traktShow);
               Console.WriteLine("Show " + traktShow.Title + " saved to DB.");
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

        internal async Task<TraktSeason[]> getSeasonsForTvShowByTVDBID(String TVDBID)
        {
            WebClient seasonClient = new WebClient();
            String jsonString = await seasonClient.DownloadStringTaskAsync(new Uri("https://api.trakt.tv/show/seasons.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDBID));
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktSeason[]));
                return (TraktSeason[])ser.ReadObject(ms);
            }
        }

        internal TraktEpisode[] getUnwatchedEpisodesForShow(String TVDB)
        {
            return this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Watched == false)).ToArray();
        }

        internal async Task<TraktEpisode> getEpisodeByTvdbAndSeasonInfo(String TVDB, String season, String episode)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            if (this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Season == season) && (t.Number == episode)).Count() > 0)
            {
                TraktEpisode traktEpisode = this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Season == season) && (t.Number == episode)).FirstOrDefault();
                Console.WriteLine("Episode " + traktEpisode.Title + " fetched from DB.");
                return traktEpisode;
            }
            else
                return await getEpisodeByTVDBThroughTrakt(TVDB, season, episode);
        }

        private async Task<TraktEpisode> getEpisodeByTVDBThroughTrakt(String TVDB, String season, String episode)
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDB + "/" + season + "/" + episode), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktWatched));
                    TraktEpisode tEpisode = ((TraktWatched)ser.ReadObject(ms)).Episode;
                    tEpisode.DownloadTime = DateTime.Now;
                    tEpisode.SeasonID = 0;
                    tEpisode.Tvdb = TVDB;
                    this.saveEpisode(tEpisode);

                   Console.WriteLine("Episode " + tEpisode.Title + " fetched from Trakt server.");
                    return tEpisode;
                }
            }
            catch (WebException)
            { }
            catch (TargetInvocationException)
            { }

            return null;
        }



        internal Boolean saveEpisode(TraktEpisode traktEpisode)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            if (this.Episodes.Where(t => (t.Tvdb == traktEpisode.Tvdb) && (t.Season == traktEpisode.Season) && (t.Number == traktEpisode.Number)).Count() > 0)
            {
                updateEpisode(traktEpisode);
               Console.WriteLine("Episode " + traktEpisode.Title + " updated to DB.");
            }
            else
            {
                this.Episodes.InsertOnSubmit(traktEpisode);
               Console.WriteLine("Episode " + traktEpisode.Title + " saved to DB.");
            }

            this.SubmitChanges(ConflictMode.FailOnFirstConflict);

            return true;
        }

        private async void updateEpisode(TraktEpisode traktEpisode)
        {
            TraktEpisode dbEpisode = await getEpisodeByTVDBThroughTrakt(traktEpisode.Tvdb, traktEpisode.Season, traktEpisode.Number);

            if (dbEpisode != null)
            {
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

        internal async void deleteEpisodeBySeasonInfo(string TVDB, string season, string episode)
        {
            TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDB, season, episode);

            this.Episodes.DeleteOnSubmit(traktEpisode);
            this.SubmitChanges(ConflictMode.FailOnFirstConflict);
        }
    }
}

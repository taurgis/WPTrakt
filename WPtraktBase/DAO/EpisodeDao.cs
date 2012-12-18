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
    public class EpisodeDao : Dao
    {
        private static EpisodeDao _Instance { get; set; }
        public static EpisodeDao Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new EpisodeDao();

                return _Instance;
            }
        }

      
        private Boolean episodeAvailableInDatabaseByTVDBAndSeasonInfo(String TVDB, String season, String episode )
        {
            try
            {
                if (!this.DatabaseExists())
                    return false;

                if (this.Episodes.Count() == 0)
                    return false;

                Boolean exists = this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Season.Equals(season)) && (t.Number.Equals(episode))).Count() > 0;

                return exists;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TraktEpisode> getShowByTVDB(String TVDB, String season, String episode)
        {
            if (episodeAvailableInDatabaseByTVDBAndSeasonInfo(TVDB, season, episode))
            {
                return this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Season.Equals(season)) && (t.Number.Equals(episode))).FirstOrDefault();
            }
            else
                return await getEpisodeByTVDBThroughTrakt(TVDB, season, episode);
        }

        private async Task<TraktEpisode> getEpisodeByTVDBThroughTrakt(String TVDB, String season, String episode)
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/episode/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb + "/" + season + "/" + episode), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktWatched));
                    TraktEpisode tEpisode = ((TraktWatched)ser.ReadObject(ms)).Episode;
                    tEpisode.DownloadTime = DateTime.Now;

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

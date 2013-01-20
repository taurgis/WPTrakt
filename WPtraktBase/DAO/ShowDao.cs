/* WPtrakt - A program for managing your movie and shows library from http://www.trakt.tv
 * Copyright (C) 2011-2013 Thomas Theunen
 * See COPYING for Details
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using System;
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
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtraktBase.Model.Trakt;
using WPtraktBase.Model.Trakt.Request;

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

        public static void DisposeDB()
        {
            _Instance = null;
        }

        #region Show

        internal async Task<TraktShow> getShowByTVDB(String TVDB)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Shows.Where(t => t.tvdb_id == TVDB).Count() > 0)
                {
                    TraktShow show = this.Shows.Where(t => t.tvdb_id == TVDB).FirstOrDefault();

                    Debug.WriteLine("Show " + show.Title + " fetched from DB.");
                    return show;
                }
                else
                    return await getShowByTVDBThroughTrakt(TVDB);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("OperationCanceledException in getShowByTVDB(" + TVDB + ").");
                return null;
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("InvalidOperationException in getShowByTVDB(" + TVDB + ").");
                return null;
            }
        }

        internal TraktShow getShowByIMDB(String IMDB)
        {
            try
            {
                TraktShow show = this.Shows.Where(t => t.imdb_id == IMDB).FirstOrDefault();

                Debug.WriteLine("Show " + show.Title + " fetched from DB.");

                return show;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("OperationCanceledException in getShowByIMDB(" + IMDB + ").");

                return null;
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("InvalidOperationException in getShowByIMDB(" + IMDB + ").");

                return null;
            }
        }

        internal async void deleteShowByTvdbId(String TVDBid)
        {
            try
            {
                TraktShow show = await getShowByTVDB(TVDBid);

                this.Episodes.DeleteAllOnSubmit(this.Episodes.Where(t => (t.Tvdb == show.tvdb_id)));
                this.Seasons.DeleteAllOnSubmit(this.Seasons.Where(t => t.Tvdb == show.tvdb_id));
                this.Shows.DeleteOnSubmit(show);

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in deleteShowByTvdbId(" + TVDBid + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in deleteShowByTvdbId(" + TVDBid + ")."); }
        }

        internal void deleteSeasonEpisodes(TraktSeason season)
        {
            if (season.SeasonEpisodes != null && season.SeasonEpisodes.Count > 0)
            {
                try
                {
                    this.Episodes.DeleteAllOnSubmit(this.Episodes.Where(t => (t.Tvdb == season.SeasonEpisodes[0].Tvdb) && (t.Season == season.Season)));
                    season.SeasonEpisodes = new EntitySet<TraktEpisode>();
                    this.SubmitChanges(ConflictMode.FailOnFirstConflict);
                }
                catch (OperationCanceledException)
                { Debug.WriteLine("OperationCanceledException in deleteSeasonEpisodes(" + season.Season + ")."); }
                catch (InvalidOperationException)
                { Debug.WriteLine("InvalidOperationException in deleteSeasonEpisodes(" + season.Season + ")."); }
            }
        }


        internal async Task<TraktEpisode[]> getSeasonFromTrakt(TraktShow show, Int16 season, TraktEpisode[] episodes)
        {
            try
            {
                var showClient = new WebClient();
                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/season.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + show.tvdb_id + "/" + season), AppUser.createJsonStringForAuthentication());

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktEpisode[]));
                    episodes = (TraktEpisode[])ser.ReadObject(ms);
                    Debug.WriteLine("Fetched season " + season + " of show " + show.Title + " from Trakt.");
                    AddEpisodesToShowSeason(show, episodes, season);
                }
                return episodes;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in GetSeasonFromTrakt(" + show.Title + ", " + season + ", " + episodes.Length + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in GetSeasonFromTrakt(" + show.Title + ", " + season + ", " + episodes.Length + ")."); }

            return new TraktEpisode[0];
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
                    Debug.WriteLine("Show " + show.Title + " fetched from Trakt Server.");
                    return show;
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getShowByTVDBThroughTrakt(" + TVDB + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getShowByTVDBThroughTrakt(" + TVDB + ")."); }

            return null;
        }

        internal void saveSeasons(TraktSeason[] seasons, String TVDB)
        {
            try
            {
                foreach (TraktSeason season in seasons)
                {
                    season.Tvdb = TVDB;
                    this.Seasons.InsertOnSubmit(season);
                }
                this.SubmitChanges();
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in saveSeasons(" + seasons.Length + ", " + TVDB + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in saveSeasons(" + seasons.Length + ", " + TVDB + ")."); }
        }

        internal void saveShow(TraktShow traktShow)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Shows.Where(t => t.tvdb_id == traktShow.tvdb_id).Count() > 0)
                {
                    updateShow(traktShow);
                    Debug.WriteLine("Show " + traktShow.Title + " updated to DB.");
                }
                else
                {
                    this.Shows.InsertOnSubmit(traktShow);
                    Debug.WriteLine("Show " + traktShow.Title + " saved to DB.");
                }

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in saveShow(" + traktShow.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in saveShow(" + traktShow.Title + ")."); }
        }

        private async void updateShow(TraktShow traktShow)
        {
            try
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
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in updateShow(" + traktShow.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in updateShow(" + traktShow.Title + ")."); }
        }

        internal async Task<TraktSeason[]> getSeasonsForTvShowByTVDBID(String TVDB)
        {
            try
            {
                WebClient seasonClient = new WebClient();
                String jsonString = await seasonClient.DownloadStringTaskAsync(new Uri("https://api.trakt.tv/show/seasons.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDB));
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktSeason[]));
                    return (TraktSeason[])ser.ReadObject(ms);
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getSeasonsForTvShowByTVDBID(" + TVDB + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getSeasonsForTvShowByTVDBID(" + TVDB + ")."); }

            return new TraktSeason[0];
        }

        internal void AddEpisodesToShowSeason(TraktShow show, TraktEpisode[] episodes, int SeasonNumber)
        {
            foreach (TraktSeason season in show.Seasons)
            {
                if (int.Parse(season.Season) == SeasonNumber)
                {
                    foreach (TraktEpisode episode in episodes)
                    {
                        episode.DownloadTime = DateTime.Now;
                        episode.SeasonID = season.SeasonID;
                        episode.Tvdb = show.tvdb_id;
                        season.SeasonEpisodes.Add(episode);
                    }
                }
            }

            saveShow(show);
        }

        internal TraktEpisode[] getUnwatchedEpisodesForShow(String TVDB)
        {
            try
            {
                return this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Watched == false)).ToArray();
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in getUnwatchedEpisodesForShow(" + TVDB + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getUnwatchedEpisodesForShow(" + TVDB + ")."); }

            return new TraktEpisode[0];
        }

        #region Watchlist

        internal async Task<Boolean> addShowToWatchlist(String TVDBID, String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();

                WatchlistAuth auth = CreateWatchListAuth(IMDBID, title, year);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

                TraktShow show = await getShowByTVDB(TVDBID);
                show.InWatchlist = true;
                saveShow(show);
                return true;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in addShowToWatchlist(" + TVDBID + ", " + IMDBID + ", " + title + ", " + year + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in addShowToWatchlist(" + TVDBID + ", " + title + ", " + year + ")."); }

            return false;
        }

        internal async Task<Boolean> removeShowFromWatchlist(String TVDBID, String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();

                WatchlistAuth auth = CreateWatchListAuth(IMDBID, title, year);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

                TraktShow show = await getShowByTVDB(TVDBID);
                show.InWatchlist = false;
                saveShow(show);

            }
            catch (WebException)
            { Debug.WriteLine("WebException in removeShowFromWatchlist(" + TVDBID + ", " + IMDBID + ", " + title + ", " + year + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in removeShowFromWatchlist(" + TVDBID + ", " + title + ", " + year + ")."); }

            return false;
        }

        private static WatchlistAuth CreateWatchListAuth(String imdbID, String title, Int16 year)
        {
            WatchlistAuth auth = new WatchlistAuth();
            auth.Shows = new TraktShow[1];
            auth.Shows[0] = new TraktShow();
            auth.Shows[0].imdb_id = imdbID;
            auth.Shows[0].Title = title;
            auth.Shows[0].year = year;
            return auth;
        }

        #endregion

        #region Seen

        internal async Task<Boolean> markShowAsSeen(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedEpisodeAuth auth = createWatchedAuth(IMDBID, title, year);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktShow show = getShowByIMDB(IMDBID);
                show.Watched = true;
                saveShow(show);

                return true;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in markShowAsSeen(" + IMDBID + ", " + title + ", " + year + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in markShowAsSeen(" + IMDBID + ", " + title + ", " + year + ")."); }

            return false;
        }

        internal async Task<Boolean> markShowSeasonAsSeen(TraktShow show, Int16 season)
        {
            try
            {
                var watchlistClient = new WebClient();
                WatchedSeasonAuth auth = new WatchedSeasonAuth();

                auth.Imdb = show.imdb_id;
                auth.Title = show.Title;
                auth.Year = show.year;
                auth.Season = season;

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/season/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedSeasonAuth), auth));

                foreach (TraktEpisode episode in this.getSeasonFromShow(show, season).SeasonEpisodes)
                {
                    episode.Watched = true;
                }

                saveShow(show);

                return true;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in markShowSeasonAsSeen(" + show.Title + ", " + season + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in markShowSeasonAsSeen(" + show.Title + ", " + season + ")."); }

            return false;
        }

        internal TraktSeason getSeasonFromShow(TraktShow show, int SeasonNumber)
        {
            foreach (TraktSeason season in show.Seasons)
            {
                if (int.Parse(season.Season) == SeasonNumber)
                    return season;
            }

            return null;
        }

        private static WatchedEpisodeAuth createWatchedAuth(String imdbID, String title, Int16 year)
        {
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();


            auth.Imdb = imdbID;
            auth.Title = title;
            auth.Year = year;

            return auth;
        }

        #endregion

        #region Shouts

        internal async Task<TraktShout[]> getShoutsForShow(String imdbID)
        {
            var showClient = new WebClient();

            String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + imdbID), AppUser.createJsonStringForAuthentication());

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                return (TraktShout[])ser.ReadObject(ms);
            }
        }

        internal async Task<Boolean> addShoutToShow(String shout, String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient shoutClient = new WebClient();
                ShoutAuth auth = new ShoutAuth();

                auth.Imdb = IMDBID;
                auth.Title = title;
                auth.Year = year;
                auth.Shout = shout;

                String jsonString = await shoutClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/shout/show/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
                return true;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in addShoutToShow(" + shout + ", " + title + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in addShoutToShow(" + shout + ", " + title + ")."); }

            return false;
        }

        #endregion

        #endregion

        #region Episode

        internal async Task<TraktEpisode> getEpisodeByTvdbAndSeasonInfo(String TVDB, String season, String episode)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Season == season) && (t.Number == episode)).Count() > 0)
                {
                    TraktEpisode traktEpisode = this.Episodes.Where(t => (t.Tvdb == TVDB) && (t.Season == season) && (t.Number == episode)).FirstOrDefault();
                    Debug.WriteLine("Episode " + traktEpisode.Title + " fetched from DB.");
                    return traktEpisode;
                }
                else
                    return await getEpisodeByTVDBThroughTrakt(TVDB, season, episode);
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in getEpisodeByTvdbAndSeasonInfo(" + TVDB + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getEpisodeByTvdbAndSeasonInfo(" + TVDB + ", " + season + ", " + episode + ")."); }

            return null;
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

                    TraktShow show = await getShowByTVDB(TVDB);

                    foreach (TraktSeason traktSeason in show.Seasons)
                    {
                        if (traktSeason.Season.Equals(season))
                            tEpisode.SeasonID = traktSeason.SeasonID;
                    }

                    tEpisode.Tvdb = TVDB;
                    this.saveEpisode(tEpisode);

                    Debug.WriteLine("Episode " + tEpisode.Title + " fetched from Trakt server.");
                    return tEpisode;
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getEpisodeByTVDBThroughTrakt(" + TVDB + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getEpisodeByTVDBThroughTrakt(" + TVDB + ", " + season + ", " + episode + ")."); }

            return null;
        }

        internal void saveEpisode(TraktEpisode traktEpisode)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Episodes.Where(t => (t.Tvdb == traktEpisode.Tvdb) && (t.Season == traktEpisode.Season) && (t.Number == traktEpisode.Number)).Count() > 0)
                {
                    updateEpisode(traktEpisode);
                    Debug.WriteLine("Episode " + traktEpisode.Title + " updated to DB.");
                }
                else
                {
                    this.Episodes.InsertOnSubmit(traktEpisode);
                    Debug.WriteLine("Episode " + traktEpisode.Title + " saved to DB.");
                }

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in saveEpisode(" + traktEpisode.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in saveEpisode(" + traktEpisode.Title + ")."); }
        }

        private async void updateEpisode(TraktEpisode traktEpisode)
        {
            try
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
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in updateEpisode(" + traktEpisode.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in updateEpisode(" + traktEpisode.Title + ")."); }
        }

        internal void deleteEpisodeBySeasonInfo(string TVDBID, string season, string episode)
        {
            try
            {
                this.Episodes.DeleteAllOnSubmit(this.Episodes.Where(t => (t.Tvdb == TVDBID) && (t.Season == season) && (t.Number == episode)));
                this.SubmitChanges(ConflictMode.FailOnFirstConflict);
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in deleteEpisodeBySeasonInfo(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in deleteEpisodeBySeasonInfo(" + TVDBID + ", " + season + ", " + episode + ")."); }
        }

        #region Watchlist

        internal async Task<Boolean> addEpisodeToWatchlist(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();

                WatchedEpisodeAuth auth = CreateWatchListAuth(IMDBID, title, year, season, episode);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(IMDBID, season, episode);
                traktEpisode.InWatchlist = true;
                saveEpisode(traktEpisode);

                return true;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in addEpisodeToWatchlist(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in addEpisodeToWatchlist(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        internal async Task<Boolean> removeEpisodeFromWatchlist(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();

                WatchedEpisodeAuth auth = CreateWatchListAuth(IMDBID, title, year, season, episode);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDBID, season, episode);
                traktEpisode.InWatchlist = false;
                saveEpisode(traktEpisode);

                return true;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in removeEpisodeFromWatchlist(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in removeEpisodeFromWatchlist(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        private static WatchedEpisodeAuth CreateWatchListAuth(String imdbID, String title, Int16 year, String season, String episode)
        {
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = season;
            auth.Episodes[0].Episode = episode;
            auth.Imdb = imdbID;
            auth.Title = title;
            auth.Year = year;

            return auth;
        }

        #endregion

        #region Checkin

        internal async Task<Boolean> checkinEpisode(String TVDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient checkinClient = new WebClient();
                CheckinAuth auth = new CheckinAuth();

                auth.tvdb_id = TVDBID;
                auth.Title = title;
                auth.year = year;
                auth.Season = Int16.Parse(season);
                auth.Episode = Int16.Parse(episode);
                auth.AppDate = AppUser.getReleaseDate();

                var assembly = Assembly.GetExecutingAssembly().FullName;
                var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];
                auth.AppVersion = fullVersionNumber;

                String jsonString = await checkinClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));

                if (jsonString.Contains("failure"))
                    return false;
                else
                    return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in checkinEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in checkinEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        #endregion

        #region Seen

        internal async Task<Boolean> markEpisodeAsSeen(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedEpisodeAuth auth = createWatchedAuth(IMDBID, title, year, season, episode);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDBID, season, episode);
                traktEpisode.Watched = true;
                saveEpisode(traktEpisode);

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in markEpisodeAsSeen(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in markEpisodeAsSeen(" + TVDBID + ", " + season + ", " + episode + ")."); }
            return false;
        }

        internal async Task<Boolean> unMarkEpisodeAsSeen(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedEpisodeAuth auth = createWatchedAuth(IMDBID, title, year, season, episode);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/episode/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDBID, season, episode);
                traktEpisode.Watched = false;
                saveEpisode(traktEpisode);

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in unMarkEpisodeAsSeen(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in unMarkEpisodeAsSeen(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        private static WatchedEpisodeAuth createWatchedAuth(String imdbID, String title, Int16 year, String season, String episode)
        {
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();
            auth.Episodes = new TraktRequestEpisode[1];
            auth.Episodes[0] = new TraktRequestEpisode();
            auth.Episodes[0].Season = season;
            auth.Episodes[0].Episode = episode;
            auth.Imdb = imdbID;
            auth.Title = title;
            auth.Year = year;

            return auth;
        }

        #endregion

        #region Shouts

        internal async Task<Boolean> addShoutToEpisode(String shout, String TVDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                ShoutAuth auth = new ShoutAuth();

                auth.Tvdb = TVDBID;
                auth.Title = title;
                auth.Year = year;
                auth.Season = Int16.Parse(season);
                auth.episode = Int16.Parse(episode);
                auth.Shout = shout;

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/shout/episode/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in addShoutToEpisode(" + shout + ", " + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in addShoutToEpisode(" + shout + ", " + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        internal async Task<TraktShout[]> getShoutsForEpisode(String TVDBID, String season, String episode)
        {
            try
            {
                var episodeClient = new WebClient();

                String jsonString = await episodeClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDBID + "/" + season + "/" + episode), AppUser.createJsonStringForAuthentication());

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                    return (TraktShout[])ser.ReadObject(ms);
                }
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in getShoutsForEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getShoutsForEpisode("  + TVDBID + ", " + season + ", " + episode + ")."); }

            return new TraktShout[0];
        }

        #endregion

        #endregion
    }
}

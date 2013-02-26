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

using Microsoft.Phone.Net.NetworkInformation;
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
using System.Windows.Media.Imaging;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtraktBase.Controllers;
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

        /// <summary>
        /// Fetches the show from Trakt when not available in the local database, once fetched 
        /// the local database will be used to fetch the data without a network connection.
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <returns>Returns the TraktShow object. If there is a network/database error, NULL will be returned.</returns>
        internal async Task<TraktShow> getShowByTVDB(String TVDBID)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Shows.Where(t => t.tvdb_id == TVDBID).Count() > 0)
                {
                    TraktShow show = this.Shows.Where(t => t.tvdb_id == TVDBID).FirstOrDefault();

                    Debug.WriteLine("Show " + show.Title + " fetched from DB.");

                    return show;
                }
                else
                    return await getShowByTVDBThroughTrakt(TVDBID);
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in getShowByTVDB(" + TVDBID + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getShowByTVDB(" + TVDBID + ")."); }

            return null;
        }

        /// <summary>
        /// Fetches the show the local database. If it is not available in the database NULL will be returned.
        /// </summary>
        /// <param name="IMDBID">The IMDB ID of the show.</param>
        /// <returns>Returns the TraktShow object. If there is a database error, NULL will be returned.</returns>
        internal TraktShow getShowByIMDB(String IMDBID)
        {
            try
            {
                TraktShow show = this.Shows.Where(t => t.imdb_id == IMDBID).FirstOrDefault();

                Debug.WriteLine("Show " + show.Title + " fetched from DB.");

                return show;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in getShowByIMDB(" + IMDBID + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getShowByIMDB(" + IMDBID + ")."); }

            return null;
        }

        /// <summary>
        /// Removes a show from the database.
        /// </summary>
        /// <param name="TVDBID">The TVDB ID from the show.</param>
        /// <returns></returns>
        internal async Task<Boolean> deleteShowByTvdbId(String TVDBID)
        {
            try
            {
                TraktShow show = await getShowByTVDB(TVDBID);

                this.Episodes.DeleteAllOnSubmit(this.Episodes.Where(t => (t.Tvdb == show.tvdb_id)));
                this.Seasons.DeleteAllOnSubmit(this.Seasons.Where(t => t.Tvdb == show.tvdb_id));
                this.Shows.DeleteOnSubmit(show);

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in deleteShowByTvdbId(" + TVDBID + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in deleteShowByTvdbId(" + TVDBID + ")."); }

            return false;
        }

        /// <summary>
        /// Removes all episodes from a season, but does not delete the season itself from the show. An empty episode
        /// list will be put into the season.
        /// </summary>
        /// <param name="season">The TraktSeason object</param>
        /// <returns>
        /// If all episodes were successfully deleted from the database TRUE is returned. If it fails FALSE
        /// is returned.
        /// </returns>
        internal Boolean deleteSeasonEpisodes(TraktSeason season)
        {
            if (season.SeasonEpisodes != null && season.SeasonEpisodes.Count > 0)
            {
                try
                {
                    this.Episodes.DeleteAllOnSubmit(this.Episodes.Where(t => (t.Tvdb == season.SeasonEpisodes[0].Tvdb) && (t.Season == season.Season)));
                    season.SeasonEpisodes = new EntitySet<TraktEpisode>();
                    this.SubmitChanges(ConflictMode.FailOnFirstConflict);

                    return true;
                }
                catch (OperationCanceledException)
                { Debug.WriteLine("OperationCanceledException in deleteSeasonEpisodes(" + season.Season + ")."); }
                catch (InvalidOperationException)
                { Debug.WriteLine("InvalidOperationException in deleteSeasonEpisodes(" + season.Season + ")."); }
            }

            return false;
        }

        /// <summary>
        /// Fetches the season from Trakt when not available in the local database, once fetched 
        /// the local database will be used to fetch the data without a network connection.
        /// </summary>
        /// <param name="show">The TraktShow object.</param>
        /// <param name="season">The season number.</param>
        /// <param name="episodes"></param>
        /// <returns></returns>
        internal async Task<TraktEpisode[]> getSeasonFromTrakt(TraktShow show, Int16 season)
        {
            try
            {
                var showClient = new WebClient();
                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/season.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + show.tvdb_id + "/" + season), AppUser.createJsonStringForAuthentication());


                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktEpisode[]));
                    TraktEpisode[] episodes = (TraktEpisode[])ser.ReadObject(ms);

                    Debug.WriteLine("Fetched season " + season + " of show " + show.Title + " from Trakt.");
                    AddEpisodesToShowSeason(show, episodes, season);

                    return episodes;
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in GetSeasonFromTrakt(" + show.Title + ", " + season + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in GetSeasonFromTrakt(" + show.Title + ", " + season + ")."); }

            return new TraktEpisode[0];
        }

        /// <summary>
        /// Fetches the JSON object from https://api.trakt.tv/show/summary.json/[KEY]
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <returns>The TraktShow object. If there is a network/database error, NULL will be returned.</returns>
        private async Task<TraktShow> getShowByTVDBThroughTrakt(String TVDBID)
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDBID), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShow));
                    TraktShow show = (TraktShow)ser.ReadObject(ms);
                    show.DownloadTime = DateTime.Now;

                    foreach (String genre in show.Genres)
                        show.GenresAsString += genre + "|";

                    if (!String.IsNullOrEmpty(show.GenresAsString))
                        show.GenresAsString = show.GenresAsString.Remove(show.GenresAsString.Length - 1);
                    show.Seasons = new EntitySet<TraktSeason>();


                    Debug.WriteLine("Show " + show.Title + " fetched from Trakt Server.");

                    saveShow(show);
                    return show;
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getShowByTVDBThroughTrakt(" + TVDBID + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getShowByTVDBThroughTrakt(" + TVDBID + ")."); }

            return null;
        }

        /// <summary>
        /// Submits all seasons to the database after setting their TVDBID correctly
        /// </summary>
        /// <param name="seasons">The array of all seasons.</param>
        /// <param name="TVDBID">The TVDB ID.</param>
        /// <returns>
        /// If the seasons have been successfully saved to the database, TRUE is returned. If an
        /// error has occured during saving, FALSE is returned.
        /// </returns>
        internal Boolean saveSeasons(TraktSeason[] seasons, String TVDBID)
        {
            try
            {
                foreach (TraktSeason season in seasons)
                {
                    season.Tvdb = TVDBID;
                    this.Seasons.InsertOnSubmit(season);
                }
                this.SubmitChanges();

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in saveSeasons(" + seasons.Length + ", " + TVDBID + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in saveSeasons(" + seasons.Length + ", " + TVDBID + ")."); }

            return false;
        }

        /// <summary>
        /// Save/update a show to the local database.
        /// </summary>
        /// <param name="traktShow">The TraktShow object.</param>
        /// <returns>Returns TRUE when saving/updating was successfull. If it fails, FALSE is returned.</returns>
        internal Boolean saveShow(TraktShow traktShow)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Shows.Where(t => t.tvdb_id == traktShow.tvdb_id).Count() == 0)
                {
                    this.Shows.InsertOnSubmit(traktShow);
                    Debug.WriteLine("Show " + traktShow.Title + " saved to DB.");
                }
                else
                    Debug.WriteLine("Show " + traktShow.Title + " updated to DB.");

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in saveShow(" + traktShow.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in saveShow(" + traktShow.Title + ")."); }

            return false;
        }

        /// <summary>
        /// Fetches all seasons from the Trakt website using URL https://api.trakt.tv/show/seasons.json/[KEY]
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <returns>An array of all seasons for that show. If an error occurs an empty array is returned.</returns>
        internal async Task<TraktSeason[]> getSeasonsForTvShowByTVDBID(String TVDBID)
        {
            try
            {
                WebClient seasonClient = new WebClient();
                String jsonString = await seasonClient.DownloadStringTaskAsync(new Uri("https://api.trakt.tv/show/seasons.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDBID));
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktSeason[]));
                    return (TraktSeason[])ser.ReadObject(ms);
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getSeasonsForTvShowByTVDBID(" + TVDBID + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getSeasonsForTvShowByTVDBID(" + TVDBID + ")."); }

            return new TraktSeason[0];
        }

        /// <summary>
        /// Links episodes to a certain season of a show in the local database.
        /// </summary>
        /// <param name="show">The TraktShow object.</param>
        /// <param name="episodes">The episodes to be added to the show season.</param>
        /// <param name="SeasonNumber">The number of the season where the episodes need to be added.</param>
        /// <returns>If the data is saved correctly TRUE is returned. If it fails FALSE is returned.</returns>
        internal Boolean AddEpisodesToShowSeason(TraktShow show, TraktEpisode[] episodes, int SeasonNumber)
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

            return saveShow(show);
        }

        /// <summary>
        /// Fetches all unwatched episodes for a show from the local database.
        /// </summary>
        /// <param name="TVDBID">The TVDB ID.</param>
        /// <returns>The array of unwatched episodes. If an error occurs, an empty array is returned.</returns>
        internal TraktEpisode[] getUnwatchedEpisodesForShow(String TVDBID)
        {
            try
            {
                return this.Episodes.Where(t => (t.Tvdb == TVDBID) && (t.Watched == false)).ToArray();
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in getUnwatchedEpisodesForShow(" + TVDBID + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getUnwatchedEpisodesForShow(" + TVDBID + ")."); }

            return new TraktEpisode[0];
        }

        #region Watchlist

        /// <summary>
        /// Adds a show to the Trakt watchlist through URL https://api.trakt.tv/show/watchlist/[KEY]
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="IMDBID">The IMDB ID of the show.</param>
        /// <param name="title">The name of the show. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the show premiered.</param>
        /// <returns>
        /// If the show was successfully added to the watchlist on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        internal async Task<Boolean> addShowToWatchlist(String TVDBID, String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchlistAuth auth = CreateWatchListAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

                TraktShow show = await getShowByTVDB(TVDBID);
                show.InWatchlist = true;

                return saveShow(show);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in addShowToWatchlist(" + TVDBID + ", " + IMDBID + ", " + title + ", " + year + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in addShowToWatchlist(" + TVDBID + ", " + title + ", " + year + ")."); }

            return false;
        }

        /// <summary>
        /// Removes a show to the Trakt watchlist through URL https://api.trakt.tv/show/unwatchlist/[KEY]
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="IMDBID">The IMDB ID of the show.</param>
        /// <param name="title">The name of the show. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the show premiered.</param>
        /// <returns>
        /// If the show was successfully removed from the watchlist on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        internal async Task<Boolean> removeShowFromWatchlist(String TVDBID, String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchlistAuth auth = CreateWatchListAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

                TraktShow show = await getShowByTVDB(TVDBID);
                show.InWatchlist = false;

                return saveShow(show);
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

        /// <summary>
        /// Marks a show as seen through URL https://api.trakt.tv/show/seen/[KEY]
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the show.</param>
        /// <param name="title">The name of the show. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the show premiered.</param>
        /// <returns>
        /// If the show was successfully marked as seen on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        internal async Task<Boolean> markShowAsSeen(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedEpisodeAuth auth = createWatchedAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktShow show = getShowByIMDB(IMDBID);
                show.Watched = true;
                return saveShow(show);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in markShowAsSeen(" + IMDBID + ", " + title + ", " + year + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in markShowAsSeen(" + IMDBID + ", " + title + ", " + year + ")."); }

            return false;
        }

        /// <summary>
        /// Marks a show season as seen through URL https://api.trakt.tv/show/season/seen/[KEY]
        /// </summary>
        /// <param name="show">The TraktShow object.</param>
        /// <param name="season">The season that has been watched.</param>
        /// <returns>
        /// If the season was successfully marked as seen on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
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

                return saveShow(show);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in markShowSeasonAsSeen(" + show.Title + ", " + season + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in markShowSeasonAsSeen(" + show.Title + ", " + season + ")."); }

            return false;
        }

        /// <summary>
        /// Gets the season object from the show based on its season number.
        /// </summary>
        /// <param name="show">The TraktShow object.</param>
        /// <param name="SeasonNumber">The season number.</param>
        /// <returns>The TraktSeason object.</returns>
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

        /// <summary>
        /// Fetches the shouts for a show through URL https://api.trakt.tv/show/shouts.json/[KEY]
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the show.</param>
        /// <returns>A list of shouts. If there was an error an empty array will be returned.</returns>
        internal async Task<TraktShout[]> getShoutsForShow(String IMDBID)
        {
            var showClient = new WebClient();
            String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + IMDBID), AppUser.createJsonStringForAuthentication());

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                return (TraktShout[])ser.ReadObject(ms);
            }
        }

        /// <summary>
        /// Adds a shout to a show through URL https://api.trakt.tv/shout/show/[KEY]
        /// </summary>
        /// <param name="shout">The shout message.</param>
        /// <param name="IMDBID">The IMDBID of the show.</param>
        /// <param name="title">The name of the show. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the show premiered.</param>
        /// <returns>
        /// If the shout was successfully added to the show on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
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

        #region Images

        /// <summary>
        /// Fetches the fanart image from trakt.tv
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="fanartUrl">The URL of the fanart image.</param>
        /// <returns>A BitmapImage of the fanart.</returns>
        internal async Task<BitmapImage> getFanartImage(String TVDBID, String fanartUrl)
        {
            String fileName = TVDBID + "background" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                Debug.WriteLine("Fetching background image for " + TVDBID + " from storage.");
                return ImageController.getImageFromStorage(fileName);
            }
            else
            {
                try
                {
                    HttpWebRequest request;

                    request = (HttpWebRequest)WebRequest.Create(new Uri(fanartUrl));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;

                    System.Net.HttpStatusCode status = webResponse.StatusCode;
                    if (status == System.Net.HttpStatusCode.OK)
                    {
                        Stream str = webResponse.GetResponseStream();

                        Debug.WriteLine("Fetching background image for " + TVDBID + " from Trakt.");
                        return ImageController.saveImage(TVDBID + "background.jpg", str, 1280, 100);
                    }
                }
                catch (WebException) { }
                catch (TargetInvocationException)
                { }
            }

            return null;
        }


        /// <summary>
        /// Fetches the screen image from trakt.tv
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="season"></param>
        /// <param name="episode"></param>
        /// <param name="screenUrl"></param>
        /// <returns>A BitmapImage of the fanart.</returns>
        internal async Task<BitmapImage> getLargeScreenImage(String TVDBID, String season, String episode, String screenUrl)
        {
            String fileName = TVDBID + season + episode + "screenlarge" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                Debug.WriteLine("Fetching large screen image for " + TVDBID + " from storage.");
                return ImageController.getImageFromStorage(fileName);
            }
            else
            {
                try
                {
                    HttpWebRequest request;

                    request = (HttpWebRequest)WebRequest.Create(new Uri(screenUrl));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;
                    System.Net.HttpStatusCode status = webResponse.StatusCode;
                    if (status == System.Net.HttpStatusCode.OK)
                    {
                        Stream str = webResponse.GetResponseStream();
                        Debug.WriteLine("Fetching large screen image for " + TVDBID + " from Trakt and saving to " + fileName + ".");
                        return ImageController.saveImage(fileName, str, 318, 90);
                    }
                }
                catch (WebException) { }
                catch (TargetInvocationException)
                { }
            }

            return null;
        }

        /// <summary>
        /// Fetches the screen image from trakt.tv
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="season"></param>
        /// <param name="episode"></param>
        /// <param name="screenUrl"></param>
        /// <returns>A BitmapImage of the fanart.</returns>
        internal async Task<BitmapImage> getSmallScreenImage(String TVDBID, String season, String episode, String screenUrl)
        {
            String fileName = TVDBID + season + episode + "screensmall" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                Debug.WriteLine("Fetching small screen image for " + TVDBID + " from storage.");
                return ImageController.getImageFromStorage(fileName);
            }
            else
            {
                try
                {
                    HttpWebRequest request;

                    request = (HttpWebRequest)WebRequest.Create(new Uri(screenUrl));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;
                    System.Net.HttpStatusCode status = webResponse.StatusCode;
                    if (status == System.Net.HttpStatusCode.OK)
                    {
                        Stream str = webResponse.GetResponseStream();
                        Debug.WriteLine("Fetching small screen image for " + TVDBID + " from Trakt and saving to " + fileName + ".");
                        return ImageController.saveImage(fileName, str, 160, 100);
                    }
                }
                catch (WebException) { }
                catch (TargetInvocationException)
                { }
            }

            return null;
        }


        internal async Task<BitmapImage> getMediumCoverImage(String ID, String screenUrl)
        {
            String fileName = ID + "medium" + ".jpg";

            if (StorageController.doesFileExist(fileName) && !StorageController.IsConnectedToWifi())
            {
                Debug.WriteLine("Fetching medium image for " + ID + " from storage.");
                return ImageController.getImageFromStorage(fileName);
            }
            else
            {
                try
                {
                    HttpWebRequest request;

                    request = (HttpWebRequest)WebRequest.Create(new Uri(screenUrl));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;
                    System.Net.HttpStatusCode status = webResponse.StatusCode;
                    if (status == System.Net.HttpStatusCode.OK)
                    {
                        Stream str = webResponse.GetResponseStream();
                        Debug.WriteLine("Fetching medium image for " + ID + " from Trakt and saving to " + fileName + ".");
                        return ImageController.saveImage(fileName, str, 160, 100);
                    }
                }
                catch (WebException) { }
                catch (TargetInvocationException)
                { }
            }

            return null;
        }

        #endregion

        #endregion

        #region Episode

        /// <summary>
        /// Fetches the episode from Trakt when not available in the local database, once fetched 
        /// the local database will be used to fetch the data without a network connection.
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>The TraktEpisode object is returned. If an error occurs NULL is returned.</returns>
        internal async Task<TraktEpisode> getEpisodeByTvdbAndSeasonInfo(String TVDBID, String season, String episode)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Episodes.Where(t => (t.Tvdb == TVDBID) && (t.Season == season) && (t.Number == episode)).Count() > 0)
                {
                    TraktEpisode traktEpisode = this.Episodes.Where(t => (t.Tvdb == TVDBID) && (t.Season == season) && (t.Number == episode)).FirstOrDefault();
                    Debug.WriteLine("Episode " + traktEpisode.Title + " fetched from DB.");
                    return traktEpisode;
                }
                else
                    return await getEpisodeByTVDBThroughTrakt(TVDBID, season, episode);
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in getEpisodeByTvdbAndSeasonInfo(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getEpisodeByTvdbAndSeasonInfo(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return null;
        }

        /// <summary>
        /// Fetches the episodes from trakt.tv.
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>Returns the TraktEpisode object, if an error occurs NULL is returned.</returns>
        private async Task<TraktEpisode> getEpisodeByTVDBThroughTrakt(String TVDBID, String season, String episode)
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDBID + "/" + season + "/" + episode), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktWatched));
                    TraktEpisode tEpisode = ((TraktWatched)ser.ReadObject(ms)).Episode;
                    tEpisode.DownloadTime = DateTime.Now;

                    TraktShow show = await getShowByTVDB(TVDBID);

                    foreach (TraktSeason traktSeason in show.Seasons)
                    {
                        if (traktSeason.Season.Equals(season))
                            tEpisode.SeasonID = traktSeason.SeasonID;
                    }

                    tEpisode.Tvdb = TVDBID;

                    Debug.WriteLine("Episode " + tEpisode.Title + " fetched from Trakt server.");

                    saveEpisode(tEpisode);
                    return tEpisode;
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getEpisodeByTVDBThroughTrakt(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getEpisodeByTVDBThroughTrakt(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return null;
        }

        /// <summary>
        /// Saves/updates an episode to the local database.
        /// </summary>
        /// <param name="traktEpisode">The TraktEpisode object.</param>
        /// <returns>
        /// If the episode has been successfully saved/updated TRUE is returned, if something goes wrong
        /// false is returned.
        /// </returns>
        internal Boolean saveEpisode(TraktEpisode traktEpisode)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Episodes.Where(t => (t.Tvdb == traktEpisode.Tvdb) && (t.Season == traktEpisode.Season) && (t.Number == traktEpisode.Number)).Count() == 0)
                {
                    this.Episodes.InsertOnSubmit(traktEpisode);
                    Debug.WriteLine("Episode " + traktEpisode.Title + " saved to DB.");
                }
                else
                    Debug.WriteLine("Episode " + traktEpisode.Title + " updated to DB.");

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in saveEpisode(" + traktEpisode.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in saveEpisode(" + traktEpisode.Title + ")."); }

            return false;
        }

        /// <summary>
        /// Deletes an episode(s) from the local database. This function also removes floating 
        /// episodes that aren't linked to anything.
        /// </summary>
        /// <param name="TVDBID">The TVDB id of the show.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>
        /// Returns TRUE if the deletion was successfull. If something goes wrong
        /// false is returned.
        /// </returns>
        internal Boolean deleteEpisodeBySeasonInfo(string TVDBID, string season, string episode)
        {
            try
            {
                this.Episodes.DeleteAllOnSubmit(this.Episodes.Where(t => (t.Tvdb == TVDBID) && (t.Season == season) && (t.Number == episode)));
                this.SubmitChanges(ConflictMode.FailOnFirstConflict);

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in deleteEpisodeBySeasonInfo(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in deleteEpisodeBySeasonInfo(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        #region Watchlist

        /// <summary>
        /// Adds an episode to the watchlist on trakt.tv.
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="IMDBID">The IMDB ID of the show.</param>
        /// <param name="title">The name of the show.</param>
        /// <param name="year">The year the show premiered.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>
        /// If the episode has successfully been added to the watchlist 
        /// TRUE is returned. If it has failed FALSE is returned.
        /// </returns>
        internal async Task<Boolean> addEpisodeToWatchlist(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();

                WatchedEpisodeAuth auth = CreateWatchListAuth(IMDBID, title, year, season, episode);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDBID, season, episode);
                traktEpisode.InWatchlist = true;

                return saveEpisode(traktEpisode);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in addEpisodeToWatchlist(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in addEpisodeToWatchlist(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        /// <summary>
        /// Removes an episode from the watchlist on trakt.tv.
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="IMDBID">The IMDB ID of the show.</param>
        /// <param name="title">The name of the show.</param>
        /// <param name="year">The year the show premiered.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>
        /// If the episode has successfully been removed from the watchlist 
        /// TRUE is returned. If it has failed FALSE is returned.
        /// </returns>
        internal async Task<Boolean> removeEpisodeFromWatchlist(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();

                WatchedEpisodeAuth auth = CreateWatchListAuth(IMDBID, title, year, season, episode);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDBID, season, episode);
                traktEpisode.InWatchlist = false;
                return saveEpisode(traktEpisode);
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

        /// <summary>
        /// Checks an episode in on trakt.tv
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="title">The name of the show.</param>
        /// <param name="year">The year the show first premiered.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>Returns true if the episode has been successfully check in, if it fails FALSE is returned.</returns>
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

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDBID, season, episode);

                traktEpisode.Watched = true;

                if (!saveEpisode(traktEpisode))
                    return false;

                if (jsonString.Contains("failure"))
                    return false;
                else
                    return true;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in checkinEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in checkinEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in checkinEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        #endregion

        #region Seen

        /// <summary>
        /// Marks an episode as seen on trakt.tv
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show</param>
        /// <param name="IMDBID">The IMDB ID of the show.</param>
        /// <param name="title">The name of the show.</param>
        /// <param name="year">The year the show first premiered.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>If the episode has been marked as seen TRUE is returned. If it fails, FALSE is returned.</returns>
        internal async Task<Boolean> markEpisodeAsSeen(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedEpisodeAuth auth = createWatchedAuth(IMDBID, title, year, season, episode);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/episode/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDBID, season, episode);
                traktEpisode.Watched = true;

                return saveEpisode(traktEpisode);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in markEpisodeAsSeen(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in markEpisodeAsSeen(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in markEpisodeAsSeen(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        /// <summary>
        /// Unmarks an episode as seen on trakt.tv
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show</param>
        /// <param name="IMDBID">The IMDB ID of the show.</param>
        /// <param name="title">The name of the show.</param>
        /// <param name="year">The year the show first premiered.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>If the episode has been unmarked as seen TRUE is returned. If it fails, FALSE is returned.</returns>
        internal async Task<Boolean> unMarkEpisodeAsSeen(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedEpisodeAuth auth = createWatchedAuth(IMDBID, title, year, season, episode);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/episode/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

                TraktEpisode traktEpisode = await getEpisodeByTvdbAndSeasonInfo(TVDBID, season, episode);
                traktEpisode.Watched = false;

                return saveEpisode(traktEpisode);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in unMarkEpisodeAsSeen(" + TVDBID + ", " + season + ", " + episode + ")."); }
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

        /// <summary>
        /// Adds a shout to a show through URL https://api.trakt.tv/shout/show/[KEY]
        /// </summary>
        /// <param name="shout">The shout message.</param>
        /// <param name="TVDBID">The TVDB ID of the episode.</param>
        /// <param name="title">The name of the episode. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the episode premiered.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>
        /// If the shout was successfully added to the episode on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
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
            catch (WebException)
            { Debug.WriteLine("WebException in addShoutToEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in addShoutToEpisode(" + shout + ", " + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in addShoutToEpisode(" + shout + ", " + TVDBID + ", " + season + ", " + episode + ")."); }

            return false;
        }

        /// <summary>
        /// Fetches the shouts for an episode through URL https://api.trakt.tv/episode/shouts.json/[KEY]
        /// </summary>
        /// <param name="TVDBID">The TVDB ID of the show.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The episode number.</param>
        /// <returns>A list of shouts. If there was an error an empty array will be returned.</returns>
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
            catch (WebException)
            { Debug.WriteLine("WebException in getShoutsForEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in getShoutsForEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in getShoutsForEpisode(" + TVDBID + ", " + season + ", " + episode + ")."); }

            return new TraktShout[0];
        }

        #endregion

        #endregion

        internal async Task<TraktShow[]> searchForShows(String searchTerm)
        {
            try
            {
                var movieClient = new WebClient();
                String jsonString = await movieClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/search/shows.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + searchTerm), AppUser.createJsonStringForAuthentication());

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShow[]));
                    TraktShow[] shows = (TraktShow[])ser.ReadObject(ms);

                    ms.Close();
                    return shows;
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in searchForShows(" + searchTerm + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in searchForShows(" + searchTerm + ")."); }

            return new TraktShow[0];
        }

        internal async Task<BitmapImage> getTrendingImage(string imageSource, Int16 width)
        {
            try
            {
                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri(imageSource));
                HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;
                System.Net.HttpStatusCode status = webResponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    Stream str = webResponse.GetResponseStream();

                    return ImageController.resizeImageWithoutSaving(str, width);
                }
            }
            catch (WebException) { }
            catch (TargetInvocationException)
            { }
            return null;
        }
    }
}

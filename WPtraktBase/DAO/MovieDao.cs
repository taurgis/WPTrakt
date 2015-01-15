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
using System.Windows.Media.Imaging;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtraktBase.Controllers;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.DAO
{
    public class MovieDao : Dao
    {
        private static MovieDao _Instance { get; set; }

        public static MovieDao Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new MovieDao();

                return _Instance;
            }
        }

        public static void DisposeDB()
        {
            _Instance = null;
        }

        #region Fetch movie and save movie

        /// <summary>
        /// Fetches the movie from Trakt when not available in the local database, once fetched 
        /// the local database will be used to fetch the data without a network connection.
        /// </summary>
        /// <param name="IMDB">The IMDB of the movie.</param>
        /// <returns>The TraktMovie object. If there is a network/database error, NULL will be returned.</returns>
        internal async Task<TraktMovie> getMovieByIMDB(String IMDB)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Movies.Where(t => t.imdb_id == IMDB).Count() > 0)
                {
                    Debug.WriteLine("Fetching movie " + IMDB + " from DB.");
                    TraktMovie movie = this.Movies.Where(t => t.imdb_id == IMDB).FirstOrDefault();
                    saveMovie(movie);

                    return movie;
                }
                else
                    return await getMovieByIMDBThroughTrakt(IMDB);
            }
            catch (OperationCanceledException)
            {
                 GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in getMovieByIMDB(" + IMDB + ").", false);
            }
            catch (InvalidOperationException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in getMovieByIMDB(" + IMDB + ").", false);
            }

            return TraktMovie.NULLMOVIE;
        }

        /// <summary>
        /// Fetches the JSON object from https://api.trakt.tv/movie/summary.json/[KEY]
        /// </summary>
        /// <param name="IMDB">The IMDB of the movie.</param>
        /// <returns>The TraktMovie object. If there is a network/database error, NULL will be returned.</returns>
        private async Task<TraktMovie> getMovieByIMDBThroughTrakt(String IMDB)
        {
            try
            {
                var movieClient = new WebClient();

                String jsonString = await movieClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + IMDB), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie));

                    TraktMovie movie = (TraktMovie)ser.ReadObject(ms);

                    Debug.WriteLine("Fetching movie " + IMDB + " from trakt.");
                    movie.DownloadTime = DateTime.Now;

                    if (movie.Genres != null && movie.Genres.Length > 0)
                    {
                        foreach (String genre in movie.Genres)
                            movie.GenresAsString += genre + "|";

                        movie.GenresAsString = movie.GenresAsString.Remove(movie.GenresAsString.Length - 1);
                    }

                    if (saveMovie(movie))
                    {
                        return movie;
                    }
                }
            }
            catch (WebException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in getMovieByIMDBThroughTrakt(" + IMDB + ").", false);
            }
            catch (OperationCanceledException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in getMovieByIMDBThroughTrakt(" + IMDB + ").", false);
            }
            catch (InvalidOperationException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in getMovieByIMDBThroughTrakt(" + IMDB + ").", false);
            }

            return TraktMovie.NULLMOVIE;
        }

        /// <summary>
        /// Save/update a movie to the local database.
        /// </summary>
        /// <param name="traktMovie">The TraktMovie object.</param>
        /// <returns>Returns TRUE when saving/updating was successfull. If it fails, FALSE is returned.</returns>
        internal Boolean saveMovie(TraktMovie traktMovie)
        {
            try
            {
                traktMovie.LastOpenedTime = DateTime.Now;

                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Movies.Where(t => t.imdb_id == traktMovie.imdb_id).Count() == 0)
                    this.Movies.InsertOnSubmit(traktMovie);

                Debug.WriteLine("Saving movie " + traktMovie.Title + " to DB.");

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);

                return true;
            }
            catch (OperationCanceledException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in saveMovie(" + traktMovie.Title + ").", false); }
            catch (InvalidOperationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in saveMovie(" + traktMovie.Title + ").", false); }

            return false;
        }

        #endregion

        #region Watchlist

        /// <summary>
        /// Adds a movie to the Trakt watchlist through URL https://api.trakt.tv/movie/watchlist/[KEY]
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the movie.</param>
        /// <param name="title">The name of the movie. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the movie premiered.</param>
        /// <returns>
        /// If the movie was successfully added to the watchlist on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        internal async Task<Boolean> addMovieToWatchlist(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchlistAuth auth = CreateWatchListAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
                TraktMovie movie = await getMovieByIMDB(IMDBID);

                movie.InWatchlist = true;

                return saveMovie(movie);
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in addMovieToWatchlist(" + IMDBID + ", " + title + ").", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in addMovieToWatchlist(" + IMDBID + ", " + title + ").", false); }

            return false;
        }

        /// <summary>
        /// Removes a movie from the Trakt watchlist through URL https://api.trakt.tv/movie/unwatchlist/[KEY]
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the movie.</param>
        /// <param name="title">The name of the movie. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the movie premiered.</param>
        /// <returns>
        /// If the movie was successfully removed from the watchlist on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        internal async Task<Boolean> removeMovieFromWatchlist(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchlistAuth auth = CreateWatchListAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
                TraktMovie movie = await getMovieByIMDB(IMDBID);

                movie.InWatchlist = false;

                return saveMovie(movie);
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in removeMovieFromWatchlist(" + IMDBID + ", " + title + ").", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in removeMovieFromWatchlist(" + IMDBID + ", " + title + ").", false); }

            return false;
        }

        private static WatchlistAuth CreateWatchListAuth(String imdbID, String title, Int16 year)
        {
            WatchlistAuth auth = new WatchlistAuth();
            auth.Movies = new TraktMovie[1];
            auth.Movies[0] = new TraktMovie();
            auth.Movies[0].imdb_id = imdbID;
            auth.Movies[0].Title = title;
            auth.Movies[0].year = year;
            return auth;
        }

        #endregion

        #region Seen

        /// <summary>
        /// Marks a movie as seen through URL https://api.trakt.tv/movie/seen/[KEY]
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the movie.</param>
        /// <param name="title">The name of the movie. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the movie premiered.</param>
        /// <returns>
        /// If the movie was successfully marked as seen on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        internal async Task<Boolean> markMovieAsSeen(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedAuth auth = createWatchedAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
                TraktMovie movie = await getMovieByIMDB(IMDBID);

                movie.Watched = true;

                return saveMovie(movie);
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in markMovieAsSeen(" + IMDBID + ", " + title + ").", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in markMovieAsSeen(" + IMDBID + ", " + title + ").", false); }

            return false;
        }

        /// <summary>
        /// Unmarks a movie as seen through URL https://api.trakt.tv/movie/unseen/[KEY]
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the movie.</param>
        /// <param name="title">The name of the movie. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the movie premiered.</param>
        /// <returns>
        /// If the movie was successfully unmarked as seen on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        internal async Task<Boolean> unMarkMovieAsSeen(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedAuth auth = createWatchedAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
                TraktMovie movie = await getMovieByIMDB(IMDBID);

                movie.Watched = false;

                return saveMovie(movie);
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in unMarkMovieAsSeen(" + IMDBID + ", " + title + ").", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in unMarkMovieAsSeen(" + IMDBID + ", " + title + ").", false); }

            return false;
        }

        private static WatchedAuth createWatchedAuth(String imdbID, String title, Int16 year)
        {
            WatchedAuth auth = new WatchedAuth();
            auth.Movies = new TraktMovieRequest[1];
            auth.Movies[0] = new TraktMovieRequest();
            auth.Movies[0].imdb_id = imdbID;
            auth.Movies[0].Title = title;
            auth.Movies[0].year = year;
            auth.Movies[0].Plays = 1;
            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            auth.Movies[0].LastPlayed = (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;

            return auth;
        }

        #endregion

        #region Checkin

        /// <summary>
        /// Checks in a movie through URL https://api.trakt.tv/movie/checkin/[KEY]
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the movie.</param>
        /// <param name="title">The name of the movie. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the movie premiered.</param>
        /// <returns>
        /// If the movie was successfully checked in on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        public async Task<Boolean> checkinMovie(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient checkinClient = new WebClient();
                String assembly = Assembly.GetExecutingAssembly().FullName;
                String fullVersionNumber = assembly.Split('=')[1].Split(',')[0];

                CheckinAuth auth = new CheckinAuth();
                auth.imdb_id = IMDBID;
                auth.Title = title;
                auth.year = year;
                auth.AppDate = AppUser.getReleaseDate();
                auth.AppVersion = fullVersionNumber;

                String jsonString = await checkinClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));

                if (jsonString.Contains("failure"))
                    return false;
                else
                    return true;
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in checkinMovie(" + IMDBID + ", " + title + ").", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in checkinMovie(" + IMDBID + ", " + title + ").", false); }
            return false;
        }

        #endregion

        #region Shouts

        /// <summary>
        /// Fetches the shouts for a movie through URL https://api.trakt.tv/movie/shouts.json/[KEY]
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the movie</param>
        /// <returns>A list of shouts. If there was an error an empty array will be returned.</returns>
        internal async Task<TraktShout[]> getShoutsForMovie(String IMDBID)
        {
            try
            {
                var movieClient = new WebClient();

                String jsonString = await movieClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + IMDBID), AppUser.createJsonStringForAuthentication());

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShout[]));

                    return (TraktShout[])ser.ReadObject(ms);
                }
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in getShoutsForMovie(" + IMDBID + ").", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in getShoutsForMovie(" + IMDBID + ").", false); }

            return new TraktShout[0];
        }

        /// <summary>
        /// Adds a shout to a movie through URL https://api.trakt.tv/shout/movie/[KEY]
        /// </summary>
        /// <param name="shout">The shout message.</param>
        /// <param name="IMDBID">The IMDBID of the movie.</param>
        /// <param name="title">The name of the movie. (Attribute is title on trakt.tv).</param>
        /// <param name="year">The year the movie premiered.</param>
        /// <returns>
        /// If the shout was successfully added to the movie on trakt.tv, it will return TRUE. If it
        /// fails FALSE will be returned.
        /// </returns>
        internal async Task<Boolean> addShoutToMovie(String shout, String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                ShoutAuth auth = new ShoutAuth();

                auth.Imdb = IMDBID;
                auth.Title = title;
                auth.Year = year;
                auth.Shout = shout;

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/shout/movie/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
                return true;
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in addShoutToMovie(" + IMDBID + ", " + title + ").", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in addShoutToMovie(" + IMDBID + ", " + title + ").", false); }
            return false;
        }

        #endregion

        #region Images

        /// <summary>
        /// Fetches the fanart image from trakt.tv
        /// </summary>
        /// <param name="IMDBID">The IMDBID of the movie.</param>
        /// <param name="fanartUrl">The URL of the fanart image.</param>
        /// <returns>A BitmapImage of the fanart.</returns>
        internal async Task<BitmapImage> getFanartImage(String IMDBID, String fanartUrl)
        {
            String fileName = IMDBID + "background" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                return ImageController.getImageFromStorage(fileName);
            }
            else
            {
                return await ImageController.FetchImageFromUrl(fanartUrl, fileName, 800);
            }
        }

       

        #endregion

        internal async Task<TraktMovie[]> getUserMovies()
        {
            try
            {
                return await getUserMoviesThroughTrakt();
            }
            catch (OperationCanceledException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in getUserMovies().", false); }
            catch (InvalidOperationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in getUserMovies().", false); }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }

        internal TraktMovie[] getRecentMovies()
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                return this.Movies.Where(t => t.LastOpenedTime > DateTime.Now.AddDays(-2)).OrderByDescending(t => t.LastOpenedTime).ToArray();
            }
            catch (OperationCanceledException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in getRecentUserMovies().", false); }
            catch (InvalidOperationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in getRecentUserMovies().", false); }

            return new TraktMovie[0];
        }

        private async Task<TraktMovie[]> getUserMoviesThroughTrakt()
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/user/library/movies/all.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
                
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                    TraktMovie[] movies = (TraktMovie[])ser.ReadObject(ms);

                    Debug.WriteLine("Fetched all shows from trakt for user.");

                    foreach (TraktMovie movie in movies)
                    {
                        movie.DownloadTime = DateTime.Now;

                        foreach (String genre in movie.Genres)
                            movie.GenresAsString += genre + "|";

                        if (!String.IsNullOrEmpty(movie.GenresAsString))
                            movie.GenresAsString = movie.GenresAsString.Remove(movie.GenresAsString.Length - 1);


                        if (movie.Ratings == null)
                            movie.Ratings = new TraktRating();

                        Debug.WriteLine("Movie " + movie.Title + " fetched from Trakt Server.");
                    }

                    return movies;
                }
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in getUserMovies().", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in getUserMovies().", false); }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }

        internal async Task<TraktMovie[]> getUserWatchlist()
        {
            try
            {
                return await getUserWatchlistThroughTrakt();
            }
            catch (OperationCanceledException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in getUserWatchlist().", false); }
            catch (InvalidOperationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in getUserWatchlist().", false); }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }

        private async Task<TraktMovie[]> getUserWatchlistThroughTrakt()
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/user/watchlist/movies.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                    TraktMovie[] movies = (TraktMovie[])ser.ReadObject(ms);

                    foreach (TraktMovie movie in movies)
                    {
                        movie.DownloadTime = DateTime.Now;

                        foreach (String genre in movie.Genres)
                            movie.GenresAsString += genre + "|";

                        if (!String.IsNullOrEmpty(movie.GenresAsString))
                            movie.GenresAsString = movie.GenresAsString.Remove(movie.GenresAsString.Length - 1);
                        

                        if (movie.Ratings == null)
                            movie.Ratings = new TraktRating();

                        Debug.WriteLine("Movie " + movie.Title + " fetched from Trakt Server.");
                    }

                    return movies;
                }
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in getUserWatchlistThroughTrakt().", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in getUserWatchlistThroughTrakt().", false); }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }

        internal async Task<TraktMovie[]> getUserSuggestions()
        {
            try
            {
                return await getUserSuggestionsThroughTrakt();
            }
            catch (OperationCanceledException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in getUserWatchlist().", false); }
            catch (InvalidOperationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in getUserWatchlist().", false); }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }

        private async Task<TraktMovie[]> getUserSuggestionsThroughTrakt()
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/recommendations/movies/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                    TraktMovie[] movies = (TraktMovie[])ser.ReadObject(ms);

                    foreach (TraktMovie movie in movies)
                    {
                        movie.DownloadTime = DateTime.Now;

                        foreach (String genre in movie.Genres)
                            movie.GenresAsString += genre + "|";

                        if (!String.IsNullOrEmpty(movie.GenresAsString))
                            movie.GenresAsString = movie.GenresAsString.Remove(movie.GenresAsString.Length - 1);
                       

                        if (movie.Ratings == null)
                            movie.Ratings = new TraktRating();

                        Debug.WriteLine("Movie " + movie.Title + " fetched from Trakt Server.");
                    }

                    return movies;
                }
            }
            catch (WebException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in getUserSuggestionsThroughTrakt().", false); }
            catch (TargetInvocationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("TargetInvocationException in getUserSuggestionsThroughTrakt().", false); }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }

        internal async Task<TraktMovie[]> FetchTrendingMovies()
        {
            try
            {
                var movieClient = new WebClient();

                String jsonString = await movieClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movies/trending.json/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    //parse into jsonser
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                    TraktMovie[] movies = (TraktMovie[])ser.ReadObject(ms);

                    return movies;
                }
            }
            catch (WebException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in FetchTrendingMovies().", false);
            }
            catch (OperationCanceledException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in FetchTrendingMovies()", false);
            }
            catch (InvalidOperationException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in FetchTrendingMovies().", false);
            }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }

        internal async Task<TraktMovie[]> searchForMovies(String searchTerm)
        {
            try
            {
                var movieClient = new WebClient();
                String jsonString = await movieClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/search/movies.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + searchTerm), AppUser.createJsonStringForAuthentication());

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie[]));
                    TraktMovie[] movies = (TraktMovie[])ser.ReadObject(ms);
                    ms.Close();

                    return movies;
                }

            }
            catch (WebException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("WebException in searchForMovies(" + searchTerm + ").", false);
            }
            catch (OperationCanceledException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in searchForMovies(" + searchTerm + ")", false);
            }
            catch (InvalidOperationException)
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in searchForMovies(" + searchTerm + ").", false);
            }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }

        internal TraktMovie[] getAllMovies()
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                return this.Movies.ToArray();
            }
            catch (OperationCanceledException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("OperationCanceledException in getAllMovies.", false); }
            catch (InvalidOperationException)
            { GoogleAnalytics.EasyTracker.GetTracker().SendException("InvalidOperationException in getAllMovies.", false); }

            return new TraktMovie[1] { TraktMovie.NULLMOVIE };
        }
    }
}

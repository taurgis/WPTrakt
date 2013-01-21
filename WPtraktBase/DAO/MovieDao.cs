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

        public async Task<TraktMovie> getMovieByIMDB(String IMDB)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Movies.Where(t => t.imdb_id == IMDB).Count() > 0)
                    return this.Movies.Where(t => t.imdb_id == IMDB).FirstOrDefault();
                else
                    return await getMovieByIMDBThroughTrakt(IMDB);
            }
            catch (WebException)
            {
                Debug.WriteLine("WebException in getMovieByIMDB(" + IMDB + ").");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("OperationCanceledException in getMovieByIMDB(" + IMDB + ").");
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("InvalidOperationException in getMovieByIMDB(" + IMDB + ").");
            }

            return null;
        }

        private async Task<TraktMovie> getMovieByIMDBThroughTrakt(String IMDB)
        {
            try
            {
                var movieClient = new WebClient();

                String jsonString = await movieClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + IMDB), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie));
                    TraktMovie Movie = (TraktMovie)ser.ReadObject(ms);
                    Movie.DownloadTime = DateTime.Now;

                    if (Movie.Genres != null && Movie.Genres.Length > 0)
                    {
                        foreach (String genre in Movie.Genres)
                            Movie.GenresAsString += genre + "|";

                        Movie.GenresAsString = Movie.GenresAsString.Remove(Movie.GenresAsString.Length - 1);
                    }
                    if (await saveMovie(Movie))
                    {
                        return Movie;
                    }
                    else
                        return null;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("OperationCanceledException in getMovieByIMDBThroughTrakt(" + IMDB + ").");
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("InvalidOperationException in getMovieByIMDBThroughTrakt(" + IMDB + ").");
            }

            return null;
        }

        public async Task<Boolean> saveMovie(TraktMovie traktMovie)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Movies.Where(t => t.imdb_id == traktMovie.imdb_id).Count() > 0)
                {
                    return await updateMovie(traktMovie);
                }
                else
                {
                    this.Movies.InsertOnSubmit(traktMovie);
                }

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in saveMovie(" + traktMovie.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in saveMovie(" + traktMovie.Title + ")."); }

            return false;
        }

        private async Task<Boolean> updateMovie(TraktMovie traktMovie)
        {
            try
            {
                TraktMovie dbMovie = await getMovieByIMDB(traktMovie.imdb_id);
                dbMovie.Certification = traktMovie.Certification;
                dbMovie.DownloadTime = traktMovie.DownloadTime;
                dbMovie.Genres = traktMovie.Genres;
                dbMovie.GenresAsString = traktMovie.GenresAsString;
                dbMovie.Images = traktMovie.Images;
                dbMovie.imdb_id = traktMovie.imdb_id;
                dbMovie.InWatchlist = traktMovie.InWatchlist;
                dbMovie.MyRating = traktMovie.MyRating;
                dbMovie.MyRatingAdvanced = traktMovie.MyRatingAdvanced;
                dbMovie.Overview = traktMovie.Overview;
                dbMovie.Ratings = traktMovie.Ratings;
                dbMovie.Released = traktMovie.Released;
                dbMovie.Runtime = traktMovie.Runtime;
                dbMovie.Tagline = traktMovie.Tagline;
                dbMovie.Title = traktMovie.Title;
                dbMovie.Tmdb = traktMovie.Tmdb;
                dbMovie.Trailer = traktMovie.Trailer;
                dbMovie.Url = traktMovie.Url;
                dbMovie.Watched = traktMovie.Watched;
                dbMovie.Watchers = traktMovie.Watchers;
                dbMovie.year = traktMovie.year;

                return true;
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in updateMovie(" + traktMovie.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in updateMovie(" + traktMovie.Title + ")."); }

            return false;
        }

        #region Watchlist

        internal async Task<Boolean> addMovieToWatchlist(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchlistAuth auth = CreateWatchListAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
                TraktMovie movie = await getMovieByIMDB(IMDBID);
                movie.InWatchlist = true;
                return await saveMovie(movie);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in addMovieToWatchlist(" + IMDBID + ", " + title + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in addMovieToWatchlist(" + IMDBID + ", " + title + ")."); }
            return false;
        }

        internal async Task<Boolean> removeMovieFromWatchlist(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchlistAuth auth = CreateWatchListAuth(IMDBID, title, year);
                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
                TraktMovie movie = await getMovieByIMDB(IMDBID);
                movie.InWatchlist = false;

                return await saveMovie(movie);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in removeMovieFromWatchlist(" + IMDBID + ", " + title + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in removeMovieFromWatchlist(" + IMDBID + ", " + title + ")."); }
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

        internal async Task<Boolean> markMovieAsSeen(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedAuth auth = createWatchedAuth(IMDBID, title, year);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));

                TraktMovie movie = await getMovieByIMDB(IMDBID);
                movie.Watched = true;
                return await saveMovie(movie);

            }
            catch (WebException)
            { Debug.WriteLine("WebException in markMovieAsSeen(" + IMDBID + ", " + title + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in markMovieAsSeen(" + IMDBID + ", " + title + ")."); }
            return false;
        }

        internal async Task<Boolean> unMarkMovieAsSeen(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient watchlistClient = new WebClient();
                WatchedAuth auth = createWatchedAuth(IMDBID, title, year);

                String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));

                TraktMovie movie = await getMovieByIMDB(IMDBID);
                movie.Watched = false;
                return await saveMovie(movie);
            }
            catch (WebException)
            { Debug.WriteLine("WebException in unMarkMovieAsSeen(" + IMDBID + ", " + title + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in unMarkMovieAsSeen(" + IMDBID + ", " + title + ")."); }
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

        public async Task<Boolean> checkinMovie(String IMDBID, String title, Int16 year)
        {
            try
            {
                WebClient checkinClient = new WebClient();
                CheckinAuth auth = new CheckinAuth();

                auth.imdb_id = IMDBID;
                auth.Title = title;
                auth.year = year;
                auth.AppDate = AppUser.getReleaseDate();

                var assembly = Assembly.GetExecutingAssembly().FullName;
                var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];
                auth.AppVersion = fullVersionNumber;

                String jsonString = await checkinClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));

                if (jsonString.Contains("failure"))
                    return false;
                else
                    return true;

            }
            catch (WebException)
            { Debug.WriteLine("WebException in checkinMovie(" + IMDBID + ", " + title + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in checkinMovie(" + IMDBID + ", " + title + ")."); }
            return false;
        }

        #endregion

        #region Shouts

        public async Task<TraktShout[]> getShoutsForMovie(String IMDBID)
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
            { Debug.WriteLine("WebException in getShoutsForMovie(" + IMDBID + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getShoutsForMovie(" + IMDBID + ")."); }
            return new TraktShout[0];
        }

        public async Task<Boolean> addShoutToMovie(String shout, String IMDBID, String title, Int16 year)
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
            { Debug.WriteLine("WebException in addShoutToMovie(" + IMDBID + ", " + title + ")."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in addShoutToMovie(" + IMDBID + ", " + title + ")."); }
            return false;
        }

        #endregion

        #region Images

        internal async Task<BitmapImage> getFanartImage(String IMDBID, String fanartUrl)
        {
            String fileName = IMDBID + "background" + ".jpg";

            if (StorageController.doesFileExist(fileName))
            {
                return ImageController.getImageFromStorage(fileName);
            }
            else
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(fanartUrl));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;

                    System.Net.HttpStatusCode status = webResponse.StatusCode;

                    if (status == System.Net.HttpStatusCode.OK)
                    {
                        Stream str = webResponse.GetResponseStream();
                        return ImageController.saveImage(fileName, str, 800, 450, 100);
                    }
                }
                catch (WebException) { }
                catch (TargetInvocationException) { }
            }

            return null;
        }

        #endregion
    }
}

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
            catch (OperationCanceledException)
            {
                Debug.WriteLine("OperationCanceledException in getMovieByIMDB(" + IMDB + ").");
                return null;
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("InvalidOperationException in getMovieByIMDB(" + IMDB + ").");
                return null;
            }
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
                    MovieDao.Instance.saveMovie(Movie);


                    return Movie;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("OperationCanceledException in getMovieByIMDBThroughTrakt(" + IMDB + ").");
                return null;
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("InvalidOperationException in getMovieByIMDBThroughTrakt(" + IMDB + ").");
                return null;
            }
        }

        public void saveMovie(TraktMovie traktMovie)
        {
            try
            {
                if (!this.DatabaseExists())
                    this.CreateDatabase();

                if (this.Movies.Where(t => t.imdb_id == traktMovie.imdb_id).Count() > 0)
                {
                    updateMovie(traktMovie);
                }
                else
                {
                    this.Movies.InsertOnSubmit(traktMovie);
                }

                this.SubmitChanges(ConflictMode.FailOnFirstConflict);

            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in saveMovie(" + traktMovie.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in saveMovie(" + traktMovie.Title + ")."); }
        }

        private async void updateMovie(TraktMovie traktMovie)
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
            }
            catch (OperationCanceledException)
            { Debug.WriteLine("OperationCanceledException in updateMovie(" + traktMovie.Title + ")."); }
            catch (InvalidOperationException)
            { Debug.WriteLine("InvalidOperationException in updateMovie(" + traktMovie.Title + ")."); }
        }
    }
}

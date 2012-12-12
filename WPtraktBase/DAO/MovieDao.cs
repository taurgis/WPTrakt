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


        private List<String> cachedMovieUUIDS;
        private Boolean movieAvailableInDatabaseByIMDB(String IMDB)
        {
            if (cachedMovieUUIDS == null)
                cachedMovieUUIDS = new List<string>();

            if (cachedMovieUUIDS.Contains(IMDB))
                return true;

            try
            {
                if (!this.DatabaseExists())
                    return false;

                if (this.Movies.Count() == 0)
                    return false;

                Boolean exists = this.Movies.Where(t => t.imdb_id == IMDB).Count() > 0;

                if (exists)
                {
                    cachedMovieUUIDS.Add(IMDB);
                }

                return exists;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TraktMovie> getMovieByIMDB(String IMDB)
        {
            if (movieAvailableInDatabaseByIMDB(IMDB))
                return this.Movies.Where(t => t.imdb_id == IMDB).FirstOrDefault();
            else
                return await getMovieByIMDBThroughTrakt(IMDB);
        }

        private async Task<TraktMovie> getMovieByIMDBThroughTrakt(String IMDB)
        {
            try
            {
                var movieClient = new WebClient();

                String jsonString = await movieClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + IMDB), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktMovie));
                    TraktMovie Movie = (TraktMovie)ser.ReadObject(ms);
                    Movie.DownloadTime = DateTime.Now;

                    foreach (String genre in Movie.Genres)
                        Movie.GenresAsString += genre + "|";

                    Movie.GenresAsString = Movie.GenresAsString.Remove(Movie.GenresAsString.Length - 1);

                    MovieDao.Instance.saveMovie(Movie);


                    return Movie;
                }
            }
            catch (WebException)
            { }
            catch (TargetInvocationException)
            { }

            return null;
        }

        public Boolean saveMovie(TraktMovie traktMovie)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            if (movieAvailableInDatabaseByIMDB(traktMovie.imdb_id))
            {
                updateMovie(traktMovie);
            }
            else
            {
                this.Movies.InsertOnSubmit(traktMovie);
            }

            this.SubmitChanges(ConflictMode.FailOnFirstConflict);

            return true;
        }

        private async void updateMovie(TraktMovie traktMovie)
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
    }
}

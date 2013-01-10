using System;
using System.Collections.Generic;
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
using WPtraktBase.DAO;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.Controller
{
    public class EpisodeController
    {
        private EpisodeDao episodeDao;

        public EpisodeController()
        {
            this.episodeDao = EpisodeDao.Instance;
        }

        public async Task<TraktEpisode> getEpisodeByTvdbAndSeasonInfo(String imdbId, String season, String episode)
        {
            return await episodeDao.getEpisodeByTvdbAndSeasonInfo(imdbId, season, episode);
        }


        /*
        public async Task<Boolean> removeMovieFromWatchlist(String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();

            WatchlistAuth auth = CreateWatchListAuth(imdbID, title, year);

            String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

            TraktMovie movie = await episodeDao.getMovieByIMDB(imdbID);
            movie.InWatchlist = false;
            episodeDao.saveMovie(movie);

            return true;
        }

        public async Task<Boolean> addMovieToWatchlist(String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();

            WatchlistAuth auth = CreateWatchListAuth(imdbID, title, year);

            String jsonString  = await  watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
           
            TraktMovie movie = await episodeDao.getMovieByIMDB(imdbID);
            movie.InWatchlist = true;
            episodeDao.saveMovie(movie);

            return true;
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

        public async Task<Boolean> checkinMovie(String imdbID, String title, Int16 year)
        {
            WebClient checkinClient = new WebClient();
            CheckinAuth auth = new CheckinAuth();

            auth.imdb_id = imdbID;
            auth.Title = title;
            auth.year = year;
            auth.AppDate = AppUser.getReleaseDate();

            var assembly = Assembly.GetExecutingAssembly().FullName;
            var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];
            auth.AppVersion = fullVersionNumber;

            String jsonString = await checkinClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));

            if (jsonString.Contains("failure"))
                return false;
            else
                return true;
        }

        public async Task<Boolean> markMovieAsSeen(String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();
            WatchedAuth auth = createWatchedAuth(imdbID, title, year);

            String jsonString =  await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
           
            TraktMovie movie = await episodeDao.getMovieByIMDB(imdbID);
            movie.Watched = true;
            episodeDao.saveMovie(movie);

            return true;
        }

        public async Task<Boolean> unMarkMovieAsSeen(String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();
            WatchedAuth auth = createWatchedAuth(imdbID, title, year);

            String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
            
            TraktMovie movie = await episodeDao.getMovieByIMDB(imdbID);
            movie.Watched = false;
            episodeDao.saveMovie(movie);

            return true;
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

        
        public async Task<Boolean> addShoutToMovie(String shout, String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();
             ShoutAuth auth = new ShoutAuth();

            auth.Imdb = imdbID;
            auth.Title = title;
            auth.Year = year;
            auth.Shout = shout;

           String jsonString  = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/shout/movie/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
           return true;
        }
         */
        public async Task<TraktShout[]> getShoutsForEpisode(String tvdb, String season, String episode)
        {
            var episodeClient = new WebClient();

            String jsonString = await episodeClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/episode/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + tvdb + "/" + season + "/" + episode), AppUser.createJsonStringForAuthentication());

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                return (TraktShout[])ser.ReadObject(ms);
            }
        }

    }
}

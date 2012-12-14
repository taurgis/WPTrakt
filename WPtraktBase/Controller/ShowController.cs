﻿using System;
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
    public class ShowController
    {
        private ShowDao showDao;

        public ShowController()
        {
            this.showDao = ShowDao.Instance;
        }

        public async Task<TraktShow> getShowByTVDBID(String TVDBID)
        {
            return await showDao.getShowByTVDB(TVDBID);
        }

        public async Task<TraktSeason[]> getSeasonsByTVDBID(String TVDBID)
        {
            return await showDao.getSeasonsForTvShowByTVDBID(TVDBID);
        }

        public void AddSeasonsToShow(TraktShow show, TraktSeason[] seasons)
        {
            show.Seasons = new System.Data.Linq.EntitySet<TraktSeason>();

            foreach (TraktSeason season in seasons)
            {
                show.Seasons.Add(season);
            }

            showDao.saveShow(show);
        }

        /*
        public async void removeShowFromWatchlist(String TVDBID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();

            WatchlistAuth auth = CreateWatchListAuth(imdbID, title, year);

            String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

            TraktMovie movie = await showDao.getMovieByIMDB(imdbID);
            movie.InWatchlist = false;
            showDao.saveMovie(movie);
        }

        public async void addMovieToWatchlist(String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();

            WatchlistAuth auth = CreateWatchListAuth(imdbID, title, year);

            String jsonString  = await  watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));
           
            TraktMovie movie = await showDao.getMovieByIMDB(imdbID);
            movie.InWatchlist = true;
            showDao.saveMovie(movie);
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

        public async void markMovieAsSeen(String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();
            WatchedAuth auth = createWatchedAuth(imdbID, title, year);

            String jsonString =  await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
           
            TraktMovie movie = await showDao.getMovieByIMDB(imdbID);
            movie.Watched = true;
            showDao.saveMovie(movie);
        }

        public async void unMarkMovieAsSeen(String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();
            WatchedAuth auth = createWatchedAuth(imdbID, title, year);

            String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));
            
            TraktMovie movie = await showDao.getMovieByIMDB(imdbID);
            movie.Watched = false;
            showDao.saveMovie(movie);
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

        public async Task<TraktShout[]> getShoutsForMovie(String imdbID)
        {
            var movieClient = new WebClient();

            String jsonString = await movieClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/movie/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + imdbID), AppUser.createJsonStringForAuthentication());

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                return (TraktShout[])ser.ReadObject(ms);
            }
        }

        public async void addShoutToMovie(String shout, String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();
             ShoutAuth auth = new ShoutAuth();

            auth.Imdb = imdbID;
            auth.Title = title;
            auth.Year = year;
            auth.Shout = shout;

           String jsonString  = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/shout/movie/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
        }
         */
    }
}

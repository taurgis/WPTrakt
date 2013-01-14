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
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtraktBase.DAO;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.Controller
{
    public class EpisodeController
    {
        private ShowDao showDao;

        public EpisodeController()
        {
            this.showDao = ShowDao.Instance;
        }

        public async Task<TraktEpisode> getEpisodeByTvdbAndSeasonInfo(String imdbId, String season, String episode, TraktShow show)
        {
            if (show.Seasons.Count == 0)
            {
                ShowController showController = new ShowController();
                TraktSeason[] seasons = await showController.getSeasonsByTVDBID(imdbId);
                foreach (TraktSeason traktSeason in seasons)
                    traktSeason.SeasonEpisodes = new EntitySet<TraktEpisode>();


                showController.AddSeasonsToShow(show, seasons);

                TraktEpisode[] episodes = await showController.getEpisodesOfSeason(show, Int16.Parse(season));

            }
            return await showDao.getEpisodeByTvdbAndSeasonInfo(imdbId, season, episode);
        }
       
       
        public async Task<Boolean> addEpisodeToWatchlist(String tvdbid, String imdbId, String title, Int16 year, String season, String episode)
        {
            WebClient watchlistClient = new WebClient();

            WatchedEpisodeAuth auth = CreateWatchListAuth(imdbId, title, year, season, episode);

            String jsonString  = await  watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/episode/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

            TraktEpisode traktEpisode = await showDao.getEpisodeByTvdbAndSeasonInfo(tvdbid, season, episode);
            traktEpisode.InWatchlist = true;
            showDao.saveEpisode(traktEpisode);

            return true;
        }

        public async Task<Boolean> removeEpisodeFromWatchlist(String tvdbid, String imdbid, String title, Int16 year, String season, String episode)
        {
            WebClient watchlistClient = new WebClient();

            WatchedEpisodeAuth auth = CreateWatchListAuth(imdbid, title, year, season, episode);

            String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/episode/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

            TraktEpisode traktEpisode = await showDao.getEpisodeByTvdbAndSeasonInfo(tvdbid, season, episode);
            traktEpisode.InWatchlist = false;
            showDao.saveEpisode(traktEpisode);

            return true;
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

       public async Task<Boolean> checkinEpisode(String tvdbid, String title, Int16 year, String season, String episode)
       {
           WebClient checkinClient = new WebClient();
           CheckinAuth auth = new CheckinAuth();

           auth.tvdb_id = tvdbid;
           auth.Title = title;
           auth.year = year;
           auth.Season = Int16.Parse(season);
           auth.Episode = Int16.Parse(episode);
           auth.AppDate = AppUser.getReleaseDate();

           var assembly = Assembly.GetExecutingAssembly().FullName;
           var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];
           auth.AppVersion = fullVersionNumber;

           String jsonString = await checkinClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/checkin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(CheckinAuth), auth));

           if (jsonString.Contains("failure"))
               return false;
           else
               return true;
       }


      public async Task<Boolean> markEpisodeAsSeen(String tvdbid, String imdbID, String title, Int16 year, String season, String episode)
      {
          WebClient watchlistClient = new WebClient();
          WatchedEpisodeAuth auth = createWatchedAuth(imdbID, title, year, season, episode);

          String jsonString =  await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/episode/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));

          TraktEpisode traktEpisode = await showDao.getEpisodeByTvdbAndSeasonInfo(tvdbid, season, episode);
          traktEpisode.Watched = true;
          showDao.saveEpisode(traktEpisode);

          return true;
      }

      public async Task<Boolean> unMarkEpisodeAsSeen(String tvdbid, String imdbID, String title, Int16 year, String season, String episode)
      {
          WebClient watchlistClient = new WebClient();
          WatchedEpisodeAuth auth = createWatchedAuth(imdbID, title, year, season, episode);

          String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/episode/unseen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedAuth), auth));

          TraktEpisode traktEpisode = await showDao.getEpisodeByTvdbAndSeasonInfo(tvdbid, season, episode);
          traktEpisode.Watched = false;
          showDao.saveEpisode(traktEpisode);

          return true;
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


      public async Task<Boolean> addShoutToEpisode(String shout, String tvdb, String title, Int16 year, String season, String episode)
      {
          WebClient watchlistClient = new WebClient();
          ShoutAuth auth = new ShoutAuth();

          auth.Tvdb = tvdb;
          auth.Title = title;
          auth.Year = year;
          auth.Season = Int16.Parse(season);
          auth.episode = Int16.Parse(episode);
          auth.Shout = shout;

          String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/shout/episode/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
          return true;
      }
    
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

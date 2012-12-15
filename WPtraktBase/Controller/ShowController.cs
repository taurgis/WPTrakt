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


        public async Task<Boolean> removeShowFromWatchlist(String TVDBID, String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();

            WatchlistAuth auth = CreateWatchListAuth(imdbID, title, year);

            String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/unwatchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

            TraktShow show = await showDao.getShowByTVDB(TVDBID);
            show.InWatchlist = false;
            showDao.saveShow(show);

            return true;
        }

        public async Task<Boolean> addShowToWatchlist(String TVDBID, String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();

            WatchlistAuth auth = CreateWatchListAuth(imdbID, title, year);

            String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/watchlist/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchlistAuth), auth));

            TraktShow show = await showDao.getShowByTVDB(TVDBID);
            show.InWatchlist = true;
            showDao.saveShow(show);

            return true;
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

        public async Task<Boolean> markShowAsSeen(String imdbID, String title, Int16 year)
        {
            WebClient watchlistClient = new WebClient();
            WatchedEpisodeAuth auth = createWatchedAuth(imdbID, title, year);

            String jsonString = await watchlistClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/seen/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(WatchedEpisodeAuth), auth));

            TraktShow show = showDao.getShowByIMDB(imdbID);
            show.Watched = true;
            showDao.saveShow(show);

            return true;
        }

        private static WatchedEpisodeAuth createWatchedAuth(String imdbID, String title, Int16 year)
        {
            WatchedEpisodeAuth auth = new WatchedEpisodeAuth();


            auth.Imdb = imdbID;
            auth.Title = title;
            auth.Year = year;

            return auth;
        }

        public async Task<TraktShout[]> getShoutsForShow(String imdbID)
        {
            var showClient = new WebClient();

            String jsonString = await showClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/shouts.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + imdbID), AppUser.createJsonStringForAuthentication());

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
            {
                var ser = new DataContractJsonSerializer(typeof(TraktShout[]));
                return (TraktShout[])ser.ReadObject(ms);
            }
        }

        public async Task<Boolean> addShoutToShow(String shout, String imdbID, String title, Int16 year)
        {
            WebClient shoutClient = new WebClient();
            ShoutAuth auth = new ShoutAuth();

            auth.Imdb = imdbID;
            auth.Title = title;
            auth.Year = year;
            auth.Shout = shout;

            String jsonString = await shoutClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/shout/show/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(ShoutAuth), auth));
            return true;
        }
    }
}

using System;
using System.Data.Linq;
using System.IO;
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
using WPtraktBase.Model.Trakt.Request;

namespace WPtraktBase.Controller
{
    public class EpisodeController
    {
        private ShowDao showDao;

        public EpisodeController()
        {
            this.showDao = ShowDao.Instance;
        }

        public async Task<TraktEpisode> getEpisodeByTvdbAndSeasonInfo(String tvdbId, String season, String episode, TraktShow show)
        {
            if (show.Seasons.Count == 0)
            {
                ShowController showController = new ShowController();
                TraktSeason[] seasons = await showController.getSeasonsByTVDBID(tvdbId);
                foreach (TraktSeason traktSeason in seasons)
                    traktSeason.SeasonEpisodes = new EntitySet<TraktEpisode>();

                await showController.AddSeasonsToShow(show, seasons);
            }

            return await showDao.getEpisodeByTvdbAndSeasonInfo(tvdbId, season, episode);
        }


        public async Task<Boolean> addEpisodeToWatchlist(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.addEpisodeToWatchlist(TVDBID, IMDBID, title, year, season, episode);
            }
            else
                return false;
        }

        public async Task<Boolean> removeEpisodeFromWatchlist(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.removeEpisodeFromWatchlist(TVDBID, IMDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> checkinEpisode(String TVDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.checkinEpisode(TVDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }


        public async Task<Boolean> markEpisodeAsSeen(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.markEpisodeAsSeen(TVDBID, IMDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> unMarkEpisodeAsSeen(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.unMarkEpisodeAsSeen(TVDBID, IMDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }


        public async Task<Boolean> addShoutToEpisode(String shout, String TVDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(shout) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.addShoutToEpisode(shout, TVDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }

        public async Task<TraktShout[]> getShoutsForEpisode(String TVDBID, String season, String episode)
        {
            return await showDao.getShoutsForEpisode(TVDBID, season, episode);
        }

        public async Task<Boolean> updateEpisode(TraktEpisode traktEpisode)
        {
            return await showDao.saveEpisode(traktEpisode);
        }
    }
}

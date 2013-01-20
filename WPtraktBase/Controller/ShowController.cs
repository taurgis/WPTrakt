﻿using System;
using System.Data.Linq;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using WPtrakt.Model.Trakt;
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
            if (!String.IsNullOrEmpty(TVDBID))
            {
                return await showDao.getShowByTVDB(TVDBID);
            }
            else
            {
                return null;
            }
        }

        public TraktShow getShowByIMDBID(String IMDBID)
        {
            if (!String.IsNullOrEmpty(IMDBID))
            {
                return showDao.getShowByIMDB(IMDBID);
            }
            else
            {
                return null;
            }
        }

        public async Task<TraktSeason[]> getSeasonsByTVDBID(String TVDBID)
        {
            if (!String.IsNullOrEmpty(TVDBID))
            {
                return await showDao.getSeasonsForTvShowByTVDBID(TVDBID);
            }
            else
            {
                return new TraktSeason[0];
            }
        }

        public async Task<Boolean> AddSeasonsToShow(TraktShow show, TraktSeason[] seasons)
        {
            if (show != null && seasons != null)
            {
                show.Seasons = new EntitySet<TraktSeason>();

                foreach (TraktSeason season in seasons)
                {
                    if (int.Parse(season.Season) > 0)
                    {
                        season.Tvdb = show.tvdb_id;
                        show.Seasons.Add(season);
                    }
                }

                return await showDao.saveShow(show);
            }

            return false;
        }

        public async Task<Boolean> deleteShow(TraktShow show)
        {
            if (show != null)
            {
                return await showDao.deleteShowByTvdbId(show.tvdb_id);
            }

            return false;
        }

        public async Task<Boolean> updateShow(TraktShow show)
        {
            if (show != null)
            {
                return await showDao.saveShow(show);
            }

            return false;
        }

        public async Task<Boolean> removeShowFromWatchlist(String TVDBID, String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await showDao.removeShowFromWatchlist(TVDBID, IMDBID, title, year);
            }

            return false;
        }

        public async Task<Boolean> addShowToWatchlist(String TVDBID, String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await showDao.addShowToWatchlist(TVDBID, IMDBID, title, year);
            }

            return false;
        }

        public async Task<TraktEpisode[]> getEpisodesOfSeason(TraktShow show, Int16 season)
        {
            if (show != null)
            {
                TraktEpisode[] episodes = null;
                TraktSeason currentSeason = showDao.getSeasonFromShow(show, season);
                if (currentSeason.SeasonEpisodes.Count > 0)
                {

                    TraktEpisode lastEpisode = currentSeason.SeasonEpisodes[currentSeason.SeasonEpisodes.Count - 1];
                    if (lastEpisode.FirstAiredAsDate >= DateTime.UtcNow && NetworkInterface.GetIsNetworkAvailable())
                    {
                        showDao.deleteSeasonEpisodes(currentSeason);
                        episodes = await showDao.getSeasonFromTrakt(show, season, episodes);
                        await showDao.AddEpisodesToShowSeason(show, episodes, season);
                    }
                    else
                    {
                        episodes = new TraktEpisode[currentSeason.SeasonEpisodes.Count];
                        Debug.WriteLine("Fetched season " + season + " of show " + show.Title + " from the DB.");
                        int i = 0;
                        foreach (TraktEpisode episode in currentSeason.SeasonEpisodes)
                            episodes[i++] = episode;
                    }
                }
                else
                {
                    episodes = await showDao.getSeasonFromTrakt(show, season, episodes);
                }

                return episodes;
            }
            else
            {
                return new TraktEpisode[0];
            }
        }

        public async Task<TraktEpisode[]> getAllUnwatchedEpisodesOfShow(TraktShow show)
        {
            if (show != null)
            {
                try
                {
                    for (Int16 season = 1; season <= show.Seasons.Count; season++)
                    {
                        TraktEpisode[] episodes = null;
                        TraktSeason currentSeason = showDao.getSeasonFromShow(show, season);
                        if (currentSeason.SeasonEpisodes.Count == 0)
                        {
                            episodes = await showDao.getSeasonFromTrakt(show, season, episodes);
                            await showDao.AddEpisodesToShowSeason(show, episodes, season);
                        }
                        else
                        {
                            if (NetworkInterface.GetIsNetworkAvailable())
                            {
                                TraktEpisode lastEpisode = currentSeason.SeasonEpisodes[currentSeason.SeasonEpisodes.Count - 1];
                                if (lastEpisode.FirstAiredAsDate >= DateTime.UtcNow)
                                {
                                    showDao.deleteSeasonEpisodes(currentSeason);
                                    episodes = await showDao.getSeasonFromTrakt(show, season, episodes);
                                    await showDao.AddEpisodesToShowSeason(show, episodes, season);
                                }
                            }
                        }
                    }

                    return showDao.getUnwatchedEpisodesForShow(show.tvdb_id);
                }
                catch (NullReferenceException)
                {
                    return new TraktEpisode[0];
                }
            }
            else
                return new TraktEpisode[0];
        }

        public async Task<Boolean> markShowAsSeen(String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await showDao.markShowAsSeen(IMDBID, title, year);
            }

            return false;
        }

        public async Task<Boolean> markShowSeasonAsSeen(TraktShow show, Int16 season)
        {
            if (show != null)
            {
               return await showDao.markShowSeasonAsSeen(show, season);
            }

            return false;
        }

        public async Task<TraktShout[]> getShoutsForShow(String IMDBID)
        {
            if (!String.IsNullOrEmpty(IMDBID))
            {
                return await showDao.getShoutsForShow(IMDBID);
            }
            else
                return new TraktShout[0];
        }

        public async Task<Boolean> addShoutToShow(String shout, String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(shout) && !String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await showDao.addShoutToShow(shout, IMDBID, title, year);
            }

            return false;
        }

        public void deleteEpisode(TraktEpisode traktEpisode)
        {
            if (traktEpisode != null)
            {
                showDao.deleteEpisodeBySeasonInfo(traktEpisode.Tvdb, traktEpisode.Season, traktEpisode.Number);
            }
        }

        public void deleteSeason(TraktShow traktShow, short season)
        {
            if (traktShow != null)
            {
                TraktSeason traktseason = showDao.getSeasonFromShow(traktShow, season);

                showDao.deleteSeasonEpisodes(traktseason);
            }
        }
    }
}

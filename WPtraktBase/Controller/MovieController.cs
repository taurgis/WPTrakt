using System;
using System.Collections.Generic;
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
using WPtraktBase.DAO;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.Controller
{
    public class MovieController
    {
        private MovieDao movieDao;

        public MovieController()
        {
            this.movieDao = MovieDao.Instance;
        }

        public async Task<TraktMovie> getMovieByImdbId(String IMDBID)
        {
            if (!String.IsNullOrEmpty(IMDBID))
            {
                return await movieDao.getMovieByIMDB(IMDBID);
            }
            else
            {
                return null;
            }
        }

        public async Task<Boolean> addMovieToWatchlist(String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await movieDao.addMovieToWatchlist(IMDBID, title, year);
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> removeMovieFromWatchlist(String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await movieDao.removeMovieFromWatchlist(IMDBID, title, year);
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> checkinMovie(String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await movieDao.checkinMovie(IMDBID, title, year);
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> markMovieAsSeen(String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await movieDao.markMovieAsSeen(IMDBID, title, year);
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> unMarkMovieAsSeen(String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await movieDao.unMarkMovieAsSeen(IMDBID, title, year);
            }
            else
            {
                return false;
            }
        }

        public async Task<TraktShout[]> getShoutsForMovie(String IMDBID)
        {
            if (!String.IsNullOrEmpty(IMDBID))
            {
                return await movieDao.getShoutsForMovie(IMDBID);
            }
            else
            {
                return new TraktShout[0];
            }
        }

        public async Task<Boolean> addShoutToMovie(String shout, String IMDBID, String title, Int16 year)
        {
            if (!String.IsNullOrEmpty(shout) && !String.IsNullOrEmpty(IMDBID) && !String.IsNullOrEmpty(title) && year > 0)
            {
                return await movieDao.addShoutToMovie(shout, IMDBID, title, year);
            }
            else
            {
                return false;
            }
        }

        public async Task<BitmapImage> getFanartImage(String IMDBID, String fanartUrl)
        {
            if (!String.IsNullOrEmpty(IMDBID))
            {
                return await movieDao.getFanartImage(IMDBID, fanartUrl);
            }
            else
            {
                return null;
            }
        }
    }
}

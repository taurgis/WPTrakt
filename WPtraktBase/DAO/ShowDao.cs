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
    public class ShowDao : Dao
    {
        private static ShowDao _Instance { get; set; }
        public static ShowDao Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new ShowDao();

                return _Instance;
            }
        }

        private List<String> cachedShowUUIDS;
        private Boolean showAvailableInDatabaseByTVDB(String TVDB)
        {
            if (cachedShowUUIDS == null)
                cachedShowUUIDS = new List<string>();

            if (cachedShowUUIDS.Contains(TVDB))
                return true;

            try
            {
                if (!this.DatabaseExists())
                    return false;

                if (this.Shows.Count() == 0)
                    return false;

                Boolean exists = this.Shows.Where(t => t.tvdb_id == TVDB).Count() > 0;

                if (exists)
                {
                    cachedShowUUIDS.Add(TVDB);
                }

                return exists;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TraktShow> getShowByTVDB(String TVDB)
        {
            if (showAvailableInDatabaseByTVDB(TVDB))
                return this.Shows.Where(t => t.tvdb_id == TVDB).FirstOrDefault();
            else
                return await getShowByTVDBThroughTrakt(TVDB);
        }

        private async Task<TraktShow> getShowByTVDBThroughTrakt(String TVDB)
        {
            try
            {
                var showClient = new WebClient();

                String jsonString = await showClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/show/summary.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + TVDB), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktShow));
                    TraktShow show = (TraktShow)ser.ReadObject(ms);
                    show.DownloadTime = DateTime.Now;

                    foreach (String genre in show.Genres)
                        show.GenresAsString += genre + "|";

                    show.GenresAsString = show.GenresAsString.Remove(show.GenresAsString.Length - 1);

                    saveShow(show);

                    return show;
                }
            }
            catch (WebException)
            { }
            catch (TargetInvocationException)
            { }

            return null;
        }

        public Boolean saveShow(TraktShow traktShow)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            if (showAvailableInDatabaseByTVDB(traktShow.tvdb_id))
            {
                updateShow(traktShow);
            }
            else
            {
                this.Shows.InsertOnSubmit(traktShow);
            }

            this.SubmitChanges(ConflictMode.FailOnFirstConflict);

            return true;
        }

        private async void updateShow(TraktShow traktShow)
        {
            TraktShow dbShow = await getShowByTVDB(traktShow.tvdb_id);
            dbShow.Certification = traktShow.Certification;
            dbShow.DownloadTime = traktShow.DownloadTime;
            dbShow.Genres = traktShow.Genres;
            dbShow.GenresAsString = traktShow.GenresAsString;
            dbShow.Images = traktShow.Images;
            dbShow.imdb_id = traktShow.imdb_id;
            dbShow.InWatchlist = traktShow.InWatchlist;
            dbShow.MyRating = traktShow.MyRating;
            dbShow.MyRatingAdvanced = traktShow.MyRatingAdvanced;
            dbShow.Overview = traktShow.Overview;
            dbShow.Ratings = traktShow.Ratings;
            dbShow.Runtime = traktShow.Runtime;
            dbShow.Title = traktShow.Title;
            dbShow.tvdb_id = traktShow.tvdb_id;
            dbShow.Url = traktShow.Url;
            dbShow.Watched = traktShow.Watched;
            dbShow.Watchers = traktShow.Watchers;
            dbShow.year = traktShow.year;
        }
    }
}

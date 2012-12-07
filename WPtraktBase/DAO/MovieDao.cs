using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.DAO
{
    public class MovieDao : Dao
    {
        public Boolean movieAvailableInDatabaseByIMDB(String IMDB)
        {
            try
            {
                if (!this.DatabaseExists())
                    return false;

                if (this.Movies.Count() == 0)
                    return false;

                return (this.Movies.Where(t => t.imdb_id == IMDB).Count() > 0);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public TraktMovie getMovieByIMDB(String IMDB)
        {
            return this.Movies.Where(t => t.imdb_id == IMDB).FirstOrDefault();
        }

        public Boolean saveMovie(TraktMovie traktMovie)
        {
            if (!this.DatabaseExists())
                this.CreateDatabase();

            this.Movies.InsertOnSubmit(traktMovie);
            this.SubmitChanges();

            return true;
        }
    }
}

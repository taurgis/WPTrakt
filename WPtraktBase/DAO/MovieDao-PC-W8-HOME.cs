using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WPtraktBase.DAO
{
    public class MovieDao : Dao
    {
        public Table<TraktMovie> Movies
        {
            get { return GetTable<TraktMovie>(); }
        }

        public TraktMovie getMovieByIMDB(String IMDB)
        {
            return this.
        }

    }
}

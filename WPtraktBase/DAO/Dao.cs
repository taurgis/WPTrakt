﻿using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using WPtrakt.Model.Trakt;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.DAO
{
    public class Dao : DataContext
    {
        

        public const string ConnectionString = "Data Source=isostore:/wptrakt.sdf";

        public Dao()
            : base(ConnectionString)
        { }

        public Table<TraktMovie> Movies
        {
            get { return GetTable<TraktMovie>(); }
        }

        public Table<TraktShow> Shows
        {
            get { return GetTable<TraktShow>(); }
        }

        public Table<TraktImage> Images
        {
            get { return GetTable<TraktImage>(); }
        }

        public Table<TraktRating> Ratings
        {
            get { return GetTable<TraktRating>(); }
        }

        public Table<TraktSeason> Seasons
        {
            get { return GetTable<TraktSeason>(); }
        }

        public Table<TraktEpisode> Episodes
        {
            get { return GetTable<TraktEpisode>(); }
        }

    }
}

/* WPtrakt - A program for managing your movie and shows library from http://www.trakt.tv
 * Copyright (C) 2011-2013 Thomas Theunen
 * See COPYING for Details
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using WPtrakt.Model.Trakt;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.DAO
{
    /// <summary>
    /// Main data access class containing all tables objects.
    /// </summary>
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

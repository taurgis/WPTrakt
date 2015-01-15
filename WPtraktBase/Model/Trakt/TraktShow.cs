using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Runtime.Serialization;
using WPtrakt.Model.Trakt;

namespace WPtraktBase.Model.Trakt
{
    [Table]
    [DataContract]
    public class TraktShow
    {
        [Column(IsDbGenerated = false, IsPrimaryKey = true)]
        [DataMember(Name = "tvdb_id")]
        public String tvdb_id { get; set; }

        [Column]
        [DataMember(Name = "imdb_id")]
        public String imdb_id { get; set; }

        [Column]
        [DataMember(Name = "title")]
        public String Title { get; set; }

        [Column]
        [DataMember(Name = "year")] 
        public Int16 year { get; set; }

        [Column]
        [DataMember(Name = "url")]
        public String Url { get; set; }

        [Column]
        [DataMember(Name = "first_aired")]
        public Int32 FirstAired { get; set; }

        [Column]
        [DataMember(Name = "country")]
        public String Country { get; set; }

        [Column]
        [DataMember(Name = "overview")]
        public String Overview { get; set; }

        [Column]
        [DataMember(Name = "runtime")]
        public Int16 Runtime { get; set; }

        [Column]
        [DataMember(Name = "network")]
        public String Network { get; set; }

        [Column]
        [DataMember(Name = "air_day")]
        public String AirDay { get; set; }

        [Column]
        [DataMember(Name = "air_time_utc")]
        public String AirTime { get; set; }

        [Column]
        [DataMember(Name = "certification")]
        public String Certification { get; set; }

        [Column]
        internal int _imageID;

        private EntityRef<TraktImage> _images;

        [Association(
            Storage = "_images",
            ThisKey = "_imageID",
            OtherKey = "ImageId",
            DeleteRule= "ON DELETE CASCADE",
            IsForeignKey = true)]
        [DataMember(Name = "images")]
        public TraktImage Images
        {
            get
            {
                try
                {
                    return _images.Entity;
                }
                catch (InvalidOperationException) {
                    return null;
                }
            }
            set
            {
                _images.Entity = value;
                if (value != null)
                {
                    _imageID = value.ImageId;
                }
            }
        }

        [Column]
        [DataMember(Name = "watchers")]
        public Int16 Watchers { get; set; }

        [Column]
        public String GenresAsString { get; set; }

        [DataMember(Name = "genres")]
        public String[] Genres { get; set; }

        [Column]
        [DataMember(Name = "in_watchlist")]
        public Boolean InWatchlist { get; set; }

        [Column]
        internal int _ratingId;
        private EntityRef<TraktRating> _ratings;

        [Association(
            Storage = "_ratings",
            ThisKey = "_ratingId",
            OtherKey = "RatingId",
            DeleteRule= "ON DELETE CASCADE",
            IsForeignKey = true)]
        [DataMember(Name = "ratings")]
        public TraktRating Ratings
        {
            get
            {
                try
                {
                    return _ratings.Entity;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
            set
            {
                _ratings.Entity = value;
                if (value != null)
                {
                    _ratingId = value.RatingId;
                }
            }
        }

        [Association(ThisKey = "tvdb_id", OtherKey = "Tvdb", DeleteRule= "ON DELETE CASCADE")]
        public EntitySet<TraktSeason> Seasons { get; set; }

        [Column]
        [DataMember(Name = "rating_advanced")]
        public Int16 MyRatingAdvanced { get; set; }

         [Column]
        [DataMember(Name = "rating")]
        public String MyRating { get; set; }

         [Column]
        [DataMember(Name = "watched")]
        public Boolean Watched { get; set; }

        [Column]
        [DataMember(Name = "DownloadTime")]
        public DateTime DownloadTime { get; set; }

        [Column]
        [DataMember(Name = "LastOpenedTime")]
        public DateTime LastOpenedTime { get; set; }

        public static String getFolderStatic()
        {
            return "show";
        }

        public  String getFolder()
        {
            return "show";
        }

        public  String getIdentifier()
        {
            return this.tvdb_id;
        }

        public TraktSeason getSeason(int season)
        {
            foreach(TraktSeason curseason in this.Seasons)
            {
                if (curseason.Season.Equals(season.ToString()))
                    return curseason;
            }

            return null;
        }

        public static TraktShow NULLSHOW
        {
            get
            {
                TraktShow nullShow = new TraktShow();
                nullShow.AirDay = DateTime.Now.ToShortDateString();
                nullShow.AirTime = DateTime.Now.ToShortTimeString();
                nullShow.Certification = "FAIL";
                nullShow.Country = "BE";
                nullShow.DownloadTime = DateTime.Now;
                nullShow.FirstAired = 0;
                nullShow.Genres = new String[1];
                nullShow.Genres[0] = "FAIL";
                nullShow.Images = TraktImage.NULLIMAGE;
                nullShow.imdb_id = "0";
                nullShow.InWatchlist = false;
                nullShow.MyRating = "0";
                nullShow.MyRatingAdvanced = 0;
                nullShow.Network = "FAIL";
                nullShow.Overview = "Something is off...";
                nullShow.Ratings = TraktRating.NULLRATING;
                nullShow.Runtime = 0;
                nullShow.Seasons = new EntitySet<TraktSeason>();
                nullShow.Seasons.Add(TraktSeason.NULLSEASON);
                nullShow.Title = "FAIL";
                nullShow.tvdb_id = "0000";
                nullShow.Url = "http://www.fail.com";
                nullShow.Watched = false;
                nullShow.Watchers = 0;
                nullShow.year = 1970;

                return nullShow;
            }
        }

        public override string ToString() {
            return this.LastOpenedTime.ToString();
        }
  
    }
}
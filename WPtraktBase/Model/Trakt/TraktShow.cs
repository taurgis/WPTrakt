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
        [DataMember(Name = "air_time")]
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
            IsForeignKey = true)]
        [DataMember(Name = "images")]
        public TraktImage Images
        {
            get { return _images.Entity; }
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
            IsForeignKey = true)]
        [DataMember(Name = "ratings")]
        public TraktRating Ratings
        {
            get { return _ratings.Entity; }
            set
            {
                _ratings.Entity = value;
                if (value != null)
                {
                    _ratingId = value.RatingId;
                }
            }
        }

        [Association(ThisKey = "tvdb_id", OtherKey = "Tvdb")]
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
    }
}
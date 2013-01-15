using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Runtime.Serialization;
using WPtrakt.Model.Trakt;

namespace WPtraktBase.Model.Trakt
{
    [Table]
    [DataContract]
    public class TraktEpisode
    {
        [Column(
          IsPrimaryKey = true,
          IsDbGenerated = true,
          DbType = "INT NOT NULL Identity",
          CanBeNull = false,
          AutoSync = AutoSync.OnInsert)]
        public int EpisodeID { get; set; }

        [Column(CanBeNull=false)]
        public int SeasonID { get; set; }

        [Column]
        [DataMember(Name = "tvdb_id")]
        public String Tvdb;

        [Column]
        [DataMember(Name = "season")]
        public String Season { get; set; }

        [Column]
        [DataMember(Name = "number")]
        public String Number { get; set; }

        [Column]
        [DataMember(Name = "title")]
        public String Title { get; set; }

        [Column]
        [DataMember(Name = "overview")]
        public String Overview { get; set; }

        [Column]
        internal int _imageID;

        private EntityRef<TraktImage> _images;

        [Association(
            Storage = "_images",
            ThisKey = "_imageID",
            DeleteRule = "ON DELETE CASCADE",
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
        [DataMember(Name = "first_aired")]
        public long FirstAired { get; set; }

        [Column]
        [DataMember(Name = "watched")]
        public Boolean Watched { get; set; }

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
            DeleteRule = "ON DELETE CASCADE",
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

        [Column]
        [DataMember(Name = "rating_advanced")]
        public Int16 MyRatingAdvanced { get; set; }

        [Column]
        [DataMember(Name = "rating")]
        public String MyRating { get; set; }


        [Column]
        [DataMember(Name = "DownloadTime")]
        public DateTime DownloadTime { get; set; }


        public String getFolder()
        {
            return "episode";
        }

        public static String getFolderStatic()
        {
            return "episode";
        }

        public  String getIdentifier()
        {
            return this.Tvdb + this.Season;
        }
    }
}

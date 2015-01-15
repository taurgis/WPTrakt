using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Runtime.Serialization;
using WPtrakt.Model.Trakt;

namespace WPtraktBase.Model.Trakt
{
    [Table]
    [DataContract]
    public class TraktMovie
    {
        [Column(IsDbGenerated = false, IsPrimaryKey = true)]
        [DataMember(Name = "imdb_id")]
        public String imdb_id { get; set; }

        [Column]
        [DataMember(Name = "title")]
        public String Title { get; set; }

        [Column]
        [DataMember(Name = "year")] 
        public Int16 year { get; set; }

        [Column]
        [DataMember(Name = "released")] 
        public String Released { get; set; }

        [Column]
        [DataMember(Name = "url")]
        public String Url { get; set; }

        [Column]
        [DataMember(Name = "trailer")]
        public String Trailer { get; set; }

        [Column]
        [DataMember(Name = "runtime")]
        public Int16 Runtime { get; set; }

        [Column]
        [DataMember(Name = "tagline")]
        public String Tagline { get; set; }

        [Column]
        [DataMember(Name = "overview")]
        public String Overview { get; set; }

        [Column]
        [DataMember(Name = "certification")]
        public String Certification { get; set; }

        [Column]
        [DataMember(Name = "tmdb_id")]
        public String Tmdb { get; set; }

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

        [Column]
        [DataMember(Name = "LastOpenedTime")]
        public DateTime LastOpenedTime { get; set; }

        public static String getFolderStatic()
        {
            return "movie";
        }

        public String getFolder()
        {
            return "movie";
        }

        public String getIdentifier()
        {
            return this.imdb_id;
        }

        public static TraktMovie NULLMOVIE
        {
            get
            {
                TraktMovie nullMovie = new TraktMovie();
                nullMovie.Certification = "FAIL";
                nullMovie.DownloadTime = DateTime.Now;
                nullMovie.Genres = new String[1] { "FAIL" };
                nullMovie.Images = TraktImage.NULLIMAGE;
                nullMovie.imdb_id = "000";
                nullMovie.InWatchlist = false;
                nullMovie.MyRating = "0";
                nullMovie.MyRatingAdvanced = 0;
                nullMovie.Overview = "What is this????";
                nullMovie.Ratings = TraktRating.NULLRATING;
                nullMovie.Released = "1970";
                nullMovie.Runtime = 0;
                nullMovie.Tagline = "No failure without fail...";
                nullMovie.Title = "FAIL";
                nullMovie.Tmdb = "000";
                nullMovie.Trailer = "http://www.fail.com";
                nullMovie.Url = "http://www.fail.com";
                nullMovie.Watched = false;
                nullMovie.Watchers = 0;
                nullMovie.year = 1970;

                return nullMovie;
            }
        }

        public override string ToString()
        {
            return this.LastOpenedTime.ToString();
        }
    }
}
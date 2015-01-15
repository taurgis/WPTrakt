using System;
using System.Data.Linq.Mapping;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [Table]
    [DataContract]
    public class TraktRating
    {
        [Column(
          IsPrimaryKey = true,
          IsDbGenerated = true,
          DbType = "INT NOT NULL Identity",
          CanBeNull = false,
          AutoSync = AutoSync.OnInsert)]
        public int RatingId { get; set; }

        [Column]
        [DataMember(Name = "percentage")]
        public Int16 Percentage { get; set; }

        [Column]
        [DataMember(Name = "votes")]
        public Int16 Votes { get; set; }

        [Column]
        [DataMember(Name = "loved")]
        public Int16 Loved { get; set; }

        [Column]
        [DataMember(Name = "hated")]
        public Int16 Hated { get; set; }

        public static TraktRating NULLRATING
        {
            get
            {
                TraktRating nullRating = new TraktRating();
                nullRating.Hated = 0;
                nullRating.Loved = 0;
                nullRating.Percentage = 0;
                nullRating.RatingId = 0;
                nullRating.Votes = 0;

                return nullRating;
            }
        }
    }
}

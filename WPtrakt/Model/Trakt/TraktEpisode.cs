using System;
using System.Runtime.Serialization;
using VPtrakt.Model.Trakt;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktEpisode : TraktObject
    {
        [DataMember(Name = "tvdb")]
        public String Tvdb;

        [DataMember(Name = "season")]
        public String Season { get; set; }

        [DataMember(Name = "number")]
        public String Number { get; set; }

        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "overview")]
        public String Overview { get; set; }

        [DataMember(Name = "images")]
        public TraktImage Images { get; set; }

        [DataMember(Name = "first_aired")]
        public long FirstAired { get; set; }

        [DataMember(Name = "watched")]
        public Boolean Watched { get; set; }

        [DataMember(Name = "in_watchlist")]
        public Boolean InWatchlist { get; set; }

        [DataMember(Name = "ratings")]
        public TraktRating Ratings { get; set; }

        [DataMember(Name = "rating_advanced")]
        public Int16 MyRatingAdvanced { get; set; }

        [DataMember(Name = "rating")]
        public String MyRating { get; set; }

        public override String getFolder()
        {
            return "episode";
        }

        public static String getFolderStatic()
        {
            return "episode";
        }

        public override String getIdentifier()
        {
            return this.Tvdb + this.Season;
        }
    }
}

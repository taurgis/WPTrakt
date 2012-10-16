using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using VPtrakt.Model.Trakt;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktShow : TraktObject
    {
        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "year")] 
        public Int16 year { get; set; }

        [DataMember(Name = "url")]
        public String Url { get; set; }

        [DataMember(Name = "first_aired")]
        public Int32 FirstAired { get; set; }

        [DataMember(Name = "country")]
        public String Country { get; set; }

        [DataMember(Name = "overview")]
        public String Overview { get; set; }

        [DataMember(Name = "runtime")]
        public Int16 Runtime { get; set; }

        [DataMember(Name = "network")]
        public String Network { get; set; }

        [DataMember(Name = "air_day")]
        public String AirDay { get; set; }

        [DataMember(Name = "air_time")]
        public String AirTime { get; set; }

        [DataMember(Name = "certification")]
        public String Certification { get; set; }

        [DataMember(Name = "imdb_id")]
        public String imdb_id { get; set; }

        [DataMember(Name = "tvdb_id")]
        public String tvdb_id { get; set; }

        [DataMember(Name = "images")]
        public TraktImage Images { get; set; }

        [DataMember(Name = "watchers")]
        public Int16 Watchers { get; set; }

        [DataMember(Name = "genres")]
        public String[] Genres { get; set; }

        [DataMember(Name = "in_watchlist")]
        public Boolean InWatchlist { get; set; }

        [DataMember(Name = "ratings")]
        public TraktRating Ratings { get; set; }

        [DataMember(Name = "rating_advanced")]
        public Int16 MyRatingAdvanced { get; set; }

        [DataMember(Name = "rating")]
        public String MyRating { get; set; }

        [DataMember(Name = "watched")]
        public Boolean Watched { get; set; }

        public static String getFolderStatic()
        {
            return "show";
        }

        public override String getFolder()
        {
            return "show";
        }

        public override String getIdentifier()
        {
            return this.tvdb_id;
        }
    }
}
/*

{
      "ratings":{
         "percentage":91,
         "votes":32,
         "loved":29,
         "hated":3
      }
   }

*/
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
    public class TraktEpisode 
    {
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

        [DataMember(Name = "ratings")]
        public TraktRating Ratings { get; set; }

        [DataMember(Name = "rating_advanced")]
        public Int16 MyRatingAdvanced { get; set; }

        [DataMember(Name = "rating")]
        public String MyRating { get; set; }
    
    }

    /*
      "episode": {
                "season": 1,
                "number": 7,
                "title": "Free Fall",
                "overview": "The FBI investigates a spectacular jewelry heist and all the clues point to Neal. Now Peter must figure out if his \"partner\" is telling the truth or if Neal is pulling a con of his own.",
                "first_aired": 1259913600,
                "url": "http://trakt.tv/show/white-collar/season/1/episode/7",
                "images": {
                    "screen": "http://trakt.us/images/episodes/25-1-7.3.jpg"
                }
            }
     */
}

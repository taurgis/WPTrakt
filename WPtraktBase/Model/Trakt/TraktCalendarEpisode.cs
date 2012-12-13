using System;
using System.Runtime.Serialization;
using WPtraktBase.Model.Trakt;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktCalendarEpisode
    {
        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }


        public DateTime Date { get; set; }
    }
}

using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktCalendar : TraktObject
    {
        [DataMember(Name = "date")]
        public String Date { get; set; }

        [DataMember(Name = "episodes")]
        public TraktCalendarEpisode[] Episodes { get; set; }
    }
}

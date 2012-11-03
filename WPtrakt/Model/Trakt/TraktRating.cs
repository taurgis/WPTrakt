using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktRating
    {
        [DataMember(Name = "percentage")]
        public Int16 Percentage { get; set; }

        [DataMember(Name = "votes")]
        public Int16 Votes { get; set; }

        [DataMember(Name = "loved")]
        public Int16 Loved { get; set; }

        [DataMember(Name = "hated")]
        public Int16 Hated { get; set; }
    }
}

using System;
using System.Runtime.Serialization;
using WPtraktBase.Model.Trakt;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktStatsDetail : TraktObject
    {

        [DataMember(Name = "watched")]
        public Int16 Watched { get; set; }

        [DataMember(Name = "watched_unique")]
        public Int16 UniqueWatched { get; set; }

        [DataMember(Name = "scrobbles")]
        public Int16 Scrobbles { get; set; }

        [DataMember(Name = "scrobbles_unique")]
        public Int16 UniqueScrobbles { get; set; }

        [DataMember(Name = "checkins")]
        public Int16 Checkins { get; set; }

        [DataMember(Name = "checkins_unique")]
        public Int16 UniqueCheckins { get; set; }

        [DataMember(Name = "seen")]
        public Int16 Seen { get; set; }

        [DataMember(Name = "unwatched")]
        public Int16 Unwatched { get; set; }

        [DataMember(Name = "collection")]
        public Int16 Collection { get; set; }

        [DataMember(Name = "shouts")]
        public Int16 Shouts { get; set; }

        [DataMember(Name = "loved")]
        public Int16 Loved { get; set; }

        [DataMember(Name = "hated")]
        public Int16 Hated { get; set; }
    }
}

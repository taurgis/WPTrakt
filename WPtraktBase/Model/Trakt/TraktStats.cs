using System;
using System.Runtime.Serialization;
using WPtraktBase.Model.Trakt;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktStats : TraktObject
    {
        [DataMember(Name = "friends")]
        public Int16 Friends { get; set; }

        [DataMember(Name = "shows")]
        public TraktStatsDetail Shows { get; set; }

        [DataMember(Name = "episodes")]
        public TraktStatsDetail Episodes { get; set; }

        [DataMember(Name = "movies")]
        public TraktStatsDetail Movies { get; set; }
    }
}

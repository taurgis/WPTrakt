using System;
using System.Runtime.Serialization;
using WPtrakt.Model.Trakt.Request;

namespace WPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class WatchedSeasonAuth : TraktRequestAuth
    {
        [DataMember(Name = "imdb_id")]
        public String Imdb { get; set; }

        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "year")]
        public Int16 Year { get; set; }

        [DataMember(Name = "season")]
        public Int16 Season { get; set; }

    }
}

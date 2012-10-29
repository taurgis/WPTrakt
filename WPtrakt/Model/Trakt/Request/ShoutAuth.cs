using System;
using System.Runtime.Serialization;

namespace VPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class ShoutAuth : TraktRequestAuth
    {
        [DataMember(Name = "imdb_id")]
        public String Imdb { get; set; }

        [DataMember(Name = "tvdb_id")]
        public String Tvdb { get; set; }

        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "year")]
        public Int16 Year { get; set; }

        [DataMember(Name = "season")]
        public Int16 Season { get; set; }

        [DataMember(Name = "episode")]
        public Int16 episode { get; set; }

        [DataMember(Name = "shout")]
        public String Shout { get; set; }

        [DataMember(Name = "spoiler")]
        public Boolean Spoiler { get; set; }
    }
}

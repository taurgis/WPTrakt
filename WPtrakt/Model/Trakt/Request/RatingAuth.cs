using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class RatingAuth : TraktRequestAuth
    {
        [DataMember(Name = "imdb_id")]
        public String Imdb { get; set; }

        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "year")]
        public Int16 Year { get; set; }

        [DataMember(Name = "rating")]
        public Int16 Rating { get; set; }

        [DataMember(Name = "season")]
        public Int16 Season { get; set; }

        [DataMember(Name = "episode")]
        public Int16 Episode { get; set; }
    }
}

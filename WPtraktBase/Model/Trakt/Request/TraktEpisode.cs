using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class TraktRequestEpisode
    {
        [DataMember(Name = "season")]
        public String Season { get; set; }

        [DataMember(Name = "episode")]
        public String Episode { get; set; }
    }

}
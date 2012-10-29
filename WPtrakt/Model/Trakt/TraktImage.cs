using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktImage
    {
        [DataMember(Name = "poster")]
        public String Poster { get; set; }

        [DataMember(Name = "fanart")]
        public String Fanart { get; set; }

        [DataMember(Name = "banner")]
        public String Banner { get; set; }

        [DataMember(Name = "screen")]
        public String Screen { get; set; }
    }
}

using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktActivity : TraktObject
    {
        [DataMember(Name = "timestamp")]
        public Int32 TimeStamp { get; set; }

        [DataMember(Name = "when")]
        public TraktWhen When { get; set; }

        [DataMember(Name = "elapsed")]
        public TraktElapsed Elapsed { get; set; }

        [DataMember(Name = "type")]
        public String Type { get; set; }

        [DataMember(Name = "action")]
        public String Action { get; set; }

        [DataMember(Name = "user")]
        public TraktProfile User { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "rating_advanced")]
        public Int16 RatingAdvanced { get; set; }
    }
}

using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktWhen
    {
        [DataMember(Name = "day")]
        public String Day { get; set; }

        [DataMember(Name = "time")]
        public String Time { get; set; }
    }
}

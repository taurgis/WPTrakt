using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktElapsed
    {
        [DataMember(Name = "full")]
        public String Full { get; set; }

        [DataMember(Name = "short")]
        public String Short { get; set; }
    }
}

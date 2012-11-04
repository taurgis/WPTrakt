using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktShout
    {
         [DataMember(Name = "inserted")]
        public String Inserted { get; set; }

        [DataMember(Name = "shout")]
        public String Shout { get; set; }

        [DataMember(Name = "spoiler")]
        public String Spoiler { get; set; }

        [DataMember(Name = "user")]
        public TraktProfile User { get; set; }
      

    }
}

using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktProgress
    {
        [DataMember(Name = "percentage")]
        public Int16 Percentage { get; set; }

        [DataMember(Name = "aired")]
        public Int16 Aired { get; set; }

        [DataMember(Name = "completed")]
        public Int16 Completed { get; set; }

        [DataMember(Name = "left")]
        public Int16 Left { get; set; }
      

    }
}

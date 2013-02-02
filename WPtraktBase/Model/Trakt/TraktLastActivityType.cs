using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WPtraktBase.Model.Trakt
{
    [DataContract]
    public class TraktLastActivityType
    {
        [DataMember(Name = "watched")]
        public Int64 Watched { get; set; }

        [DataMember(Name = "seen")]
        public Int64 Seen { get; set; }

        [DataMember(Name = "scrobble")]
        public Int64 Scrobble { get; set; }

        [DataMember(Name = "checkin")]
        public Int64 Checkin { get; set; }

        [DataMember(Name = "collection")]
        public Int64 Collection { get; set; }
    }
}

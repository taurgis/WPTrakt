using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using WPtraktBase.Model.Trakt;


namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktProfileWithWatching : TraktProfile
    {
        [DataMember(Name = "watching")]
        public TraktWatched Watching { get; set; }
    }
}

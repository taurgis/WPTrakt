using System.Runtime.Serialization;
using WPtrakt.Model.Trakt;

namespace VPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class WatchlistAuth : TraktRequestAuth
    {
        [DataMember(Name = "movies")]
        public TraktMovie[] Movies { get; set; }
        [DataMember(Name = "shows")]
        public TraktShow[] Shows { get; set; }
    }
}

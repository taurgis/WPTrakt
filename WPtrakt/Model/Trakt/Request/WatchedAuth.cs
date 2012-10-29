using System.Runtime.Serialization;
using WPtrakt.Model.Trakt.Request;

namespace VPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class WatchedAuth : TraktRequestAuth
    {
         [DataMember(Name = "movies")]
        public TraktMovieRequest[] Movies { get; set; }
    }
}

using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class FollowAuth : TraktRequestAuth
    {
        [DataMember(Name = "user")]
        public String User { get; set; }

    }
}

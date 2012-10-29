using System;
using System.Runtime.Serialization;

namespace VPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class RegisterAuth : TraktRequestAuth
    {
        [DataMember(Name = "email")]
        public String Email { get; set; }

    }
}

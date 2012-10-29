﻿using System;
using System.Runtime.Serialization;

namespace VPtrakt.Model.Trakt.Request
{
    [DataContract]
    public abstract class TraktRequestAuth
    {
        [DataMember(Name = "username")]
        public String Username { get; set; }

        [DataMember(Name = "password")]
        public String Password { get; set; }


    }
}

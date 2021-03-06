﻿using System;
using System.Runtime.Serialization;
using WPtrakt.Model.Trakt.Request;

namespace WPtraktBase.Model.Trakt.Request
{
    [DataContract]
    public class WatchedEpisodeAuth : TraktRequestAuth
    {
        [DataMember(Name = "imdb_id")]
        public String Imdb { get; set; }

        [DataMember(Name = "tvdb_id")]
        public String Tvdb { get; set; }

        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "year")]
        public Int16 Year { get; set; }

        [DataMember(Name = "episodes")]
        public TraktRequestEpisode[] Episodes { get; set; }
    }
}

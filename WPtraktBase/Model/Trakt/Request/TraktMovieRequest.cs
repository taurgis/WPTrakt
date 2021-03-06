﻿using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class TraktMovieRequest
    {
        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "year")] 
        public Int16 year { get; set; }

        [DataMember(Name = "imdb_id")]
        public String imdb_id { get; set; }

        [DataMember(Name = "last_played")]
        public Int64 LastPlayed { get; set; }

        [DataMember(Name = "plays")]
        public Int16 Plays { get; set; }
    }
}
/*

{
      "ratings":{
         "percentage":91,
         "votes":32,
         "loved":29,
         "hated":3
      }
   }

*/
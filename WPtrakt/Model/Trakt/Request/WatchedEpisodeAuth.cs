﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WPtrakt.Model.Trakt;
using System.Runtime.Serialization;
using WPtrakt.Model.Trakt.Request;

namespace VPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class WatchedEpisodeAuth : TraktRequestAuth
    {
        [DataMember(Name = "imdb_id")]
        public String Imdb { get; set; }

        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "year")]
        public Int16 Year { get; set; }

        [DataMember(Name = "episodes")]
        public TraktRequestEpisode[] Episodes { get; set; }
    }
}

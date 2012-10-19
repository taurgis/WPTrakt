using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using VPtrakt.Model.Trakt;
using VPtrakt.Model.Trakt.Request;

namespace WPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class CheckinAuth : TraktRequestAuth
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

        [DataMember(Name = "app_version")]
        public String AppVersion { get; set; }

        [DataMember(Name = "app_date")]
        public String AppDate { get; set; }
    }
}
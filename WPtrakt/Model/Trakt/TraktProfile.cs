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

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktProfile
    {
        [DataMember(Name = "username")]
        public String Username { get; set; }

        [DataMember(Name = "protected")]
        public Boolean Protected { get; set; }

        [DataMember(Name = "full_name")]
        public String Name { get; set; }

        [DataMember(Name = "gender")]
        public String Gender { get; set; }

        [DataMember(Name = "age")]
        public String Age { get; set; }

        [DataMember(Name = "location")]
        public String Location { get; set; }

        [DataMember(Name = "about")]
        public String About { get; set; }

        [DataMember(Name = "avatar")]
        public String Avatar { get; set; }

        [DataMember(Name = "url")]
        public String Url { get; set; }

        [DataMember(Name = "watched")]
        public TraktWatched[] Watched {get; set;}
    }
}

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

namespace WPtrakt.Model.Trakt.Request
{
    [DataContract]
    public class TraktRequestEpisode
    {
        [DataMember(Name = "season")]
        public String Season { get; set; }

        [DataMember(Name = "episode")]
        public String Episode { get; set; }
    }

}
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

namespace WPtraktLiveTile
{
    [DataContract]
    public class TraktImage
    {
        [DataMember(Name = "poster")]
        public String Poster { get; set; }

        [DataMember(Name = "fanart")]
        public String Fanart { get; set; }

        [DataMember(Name = "banner")]
        public String Banner { get; set; }

        [DataMember(Name = "screen")]
        public String Screen { get; set; }
    }
}

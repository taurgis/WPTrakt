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
    public class TraktShout
    {
         [DataMember(Name = "inserted")]
        public String Inserted { get; set; }

        [DataMember(Name = "shout")]
        public String Shout { get; set; }

        [DataMember(Name = "spoiler")]
        public String Spoiler { get; set; }

        [DataMember(Name = "user")]
        public TraktProfile User { get; set; }
      

    }
}

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
    public class TraktElapsed
    {
        [DataMember(Name = "full")]
        public String Full { get; set; }

        [DataMember(Name = "short")]
        public String Short { get; set; }
    }
}

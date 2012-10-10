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

namespace VPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktRating
    {
        [DataMember(Name = "percentage")]
        public Int16 Percentage { get; set; }

        [DataMember(Name = "votes")]
        public Int16 Votes { get; set; }

        [DataMember(Name = "loved")]
        public Int16 Loved { get; set; }

        [DataMember(Name = "hated")]
        public Int16 Hated { get; set; }
    }
}

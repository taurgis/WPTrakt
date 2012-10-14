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
    public abstract class TraktRequestAuth
    {
        [DataMember(Name = "username")]
        public String Username { get; set; }

        [DataMember(Name = "password")]
        public String Password { get; set; }


    }
}

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
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktCalendar
    {
        [DataMember(Name = "date")]
        public String Date { get; set; }

        [DataMember(Name = "episodes")]
        public TraktCalendarEpisode[] Episodes { get; set; }
    }
}

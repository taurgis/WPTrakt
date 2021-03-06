﻿using System;
using System.Runtime.Serialization;
using WPtraktBase.Model.Trakt;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktActivity : TraktObject, IComparable<TraktActivity>
    {
        [DataMember(Name = "timestamp")]
        public Int32 TimeStamp { get; set; }

        [DataMember(Name = "when")]
        public TraktWhen When { get; set; }

        [DataMember(Name = "elapsed")]
        public TraktElapsed Elapsed { get; set; }

        [DataMember(Name = "type")]
        public String Type { get; set; }

        [DataMember(Name = "action")]
        public String Action { get; set; }

        [DataMember(Name = "user")]
        public TraktProfile User { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }

        [DataMember(Name = "episodes")]
        public TraktEpisode[] Episodes { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "rating_advanced")]
        public Int16 RatingAdvanced { get; set; }

        public Int32 CompareTo(TraktActivity other)
        {
            return this.TimeStamp.CompareTo(other.TimeStamp);
        }



        public static Comparison<TraktActivity> ActivityComparison =
               delegate(TraktActivity p1, TraktActivity p2)
               {
                   return p2.TimeStamp.CompareTo(p1.TimeStamp);
               };
        

    }
}

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
    public class TraktWatched : TraktObject
    {
        [DataMember(Name = "type")]
        public String Type { get; set; }

        [DataMember(Name = "action")]
        public String Action { get; set; }

        [DataMember(Name = "watched")]
        public String Watched { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }


        public static String getFolderStatic()
        {
            return "episode";
        }

        public override String getFolder()
        {
            return "episode";
        }

        public override String getIdentifier()
        {
            return this.Show.tvdb_id + this.Episode.Season + this.Episode.Number;
        }
    }
}

using System;
using System.Runtime.Serialization;
using WPtrakt.Model.Trakt;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.Model.Trakt
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

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }


        public static String getFolderStatic()
        {
            return "episodesingle";
        }

        public override String getFolder()
        {
            return "episodesingle";
        }

        public override String getIdentifier()
        {
            return this.Show.tvdb_id + this.Episode.Season + this.Episode.Number;
        }
    }
}

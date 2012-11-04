using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktSeason : TraktObject
    {
        [DataMember(Name = "tvdb")]
        public String Tvdb;

        [DataMember(Name = "season")]
        public String Season { get; set; }

        [DataMember(Name = "episodes")]
        public String Episodes { get; set; }

        [DataMember(Name = "url")]
        public String Url { get; set; }

        [DataMember(Name = "images")]
        public TraktImage Images { get; set; }


        public override String getFolder()
        {
            return "season";
        }

        public static String getFolderStatic()
        {
            return "season";
        }

        public override String getIdentifier()
        {
            return this.Tvdb;
        }
    }

}

using System;
using System.Runtime.Serialization;
using WPtraktBase.Model.Trakt;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktProfile : TraktObject
    {
        [DataMember(Name = "username")]
        public String Username { get; set; }

        [DataMember(Name = "protected")]
        public Boolean Protected { get; set; }

        [DataMember(Name = "full_name")]
        public String Name { get; set; }

        [DataMember(Name = "gender")]
        public String Gender { get; set; }

        [DataMember(Name = "age")]
        public String Age { get; set; }

        [DataMember(Name = "location")]
        public String Location { get; set; }

        [DataMember(Name = "about")]
        public String About { get; set; }

        [DataMember(Name = "avatar")]
        public String Avatar { get; set; }

        [DataMember(Name = "url")]
        public String Url { get; set; }
      
        [DataMember(Name = "watched")]
        public TraktWatched[] Watched {get; set;}

        [DataMember(Name = "stats")]
        public TraktStats Stats { get; set; }

        public static String getFolderStatic()
        {
            return "profile";
        }

        public override String getFolder()
        {
            return "profile";
        }

        public override String getIdentifier()
        {
            return this.Username;
        }
    }
}

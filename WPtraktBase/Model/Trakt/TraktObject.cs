using System;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public abstract class TraktObject
    {
        [DataMember(Name = "DownloadTime")]
        public DateTime DownloadTime { get; set; }


        public virtual  String getFolder()
        {
            return "";
        }

        public virtual  String getIdentifier()
        {
            return "";
        }
    }
}

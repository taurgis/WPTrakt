using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [DataContract]
    public class TraktFriendsActivity : TraktObject
    {
        [DataMember(Name = "activity")]
        public TraktActivity[] Activity { get; set; }
    }
}

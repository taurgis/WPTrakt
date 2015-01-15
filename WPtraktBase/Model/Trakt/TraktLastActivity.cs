using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WPtraktBase.Model.Trakt
{
    [DataContract]
    public class TraktLastActivity
    {
        [DataMember(Name = "movie")]
        public TraktLastActivityType Movie { get; set; }

        [DataMember(Name = "episode")]
        public TraktLastActivityType Episode { get; set; }
    }
}

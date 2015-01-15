using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WPtrakt.Model.Trakt;

namespace WPtraktBase.Model.Trakt
{
    [DataContract]
    public class TraktShowProgress
    {
        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "progress")]
        public TraktProgress Progress { get; set; }
    }
}

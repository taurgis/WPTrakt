using System;
using System.Data.Linq.Mapping;
using System.Runtime.Serialization;

namespace WPtrakt.Model.Trakt
{
    [Table]
    [DataContract]
    public class TraktImage
    {
        [Column(
             IsPrimaryKey = true,
             IsDbGenerated = true,
             DbType = "INT NOT NULL Identity",
             CanBeNull = false,
             AutoSync = AutoSync.OnInsert)]
        public int ImageId { get; set; }

        [Column]
        [DataMember(Name = "poster")]
        public String Poster { get; set; }

        [Column]
        [DataMember(Name = "fanart")]
        public String Fanart { get; set; }

        [Column]
        [DataMember(Name = "banner")]
        public String Banner { get; set; }

        [Column]
        [DataMember(Name = "screen")]
        public String Screen { get; set; }

        public static TraktImage NULLIMAGE
        {
            get
            {
                TraktImage nullImage = new TraktImage();
                nullImage.Poster = "http://slurm.trakt.us/images/poster-dark.jpg";
                nullImage.Screen = "http://slurm.trakt.us/images/episode-dark.jpg";
                nullImage.Fanart = "http://slurm.trakt.us/images/fanart-dark.jpg";
                nullImage.Banner = "http://slurm.trakt.us/images/banner.jpg";

                return nullImage;
            }
        }
    }
}

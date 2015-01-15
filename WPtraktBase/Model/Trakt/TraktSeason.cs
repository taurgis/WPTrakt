using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Runtime.Serialization;
using WPtrakt.Model.Trakt;

namespace WPtraktBase.Model.Trakt
{
    [Table]
    [DataContract]
    public class TraktSeason
    {
        [Column(
           IsPrimaryKey = true,
           IsDbGenerated = true,
           DbType = "INT NOT NULL Identity",
           CanBeNull = false,
           AutoSync = AutoSync.OnInsert)]
        public int SeasonID { get; set; }

        [Column]
        [DataMember(Name = "tvdb")]
        public String Tvdb;

        [Column]
        [DataMember(Name = "season")]
        public String Season { get; set; }

        [Column]
        [DataMember(Name = "episodes")]
        public String Episodes { get; set; }

        [Association(ThisKey = "SeasonID", OtherKey = "SeasonID", DeleteRule= "ON DELETE CASCADE")]
        public EntitySet<TraktEpisode> SeasonEpisodes { get; set; }

        [Column]
        [DataMember(Name = "url")]
        public String Url { get; set; }

        [Column]
        internal int _imageID;

        private EntityRef<TraktImage> _images;

        [Association(
            Storage = "_images",
            ThisKey = "_imageID",
            DeleteRule = "ON DELETE CASCADE",
            OtherKey = "ImageId",
            IsForeignKey = true)]
        [DataMember(Name = "images")]
        public TraktImage Images
        {
            get { return _images.Entity; }
            set
            {
                _images.Entity = value;
                if (value != null)
                {
                    _imageID = value.ImageId;
                }
            }
        }


        public  String getFolder()
        {
            return "season";
        }

        public static String getFolderStatic()
        {
            return "season";
        }

        public  String getIdentifier()
        {
            return this.Tvdb;
        }

        public static TraktSeason NULLSEASON
        {
            get
            {
                TraktSeason nullSeason = new TraktSeason();
                nullSeason.Episodes = "0";
                nullSeason.Images = TraktImage.NULLIMAGE;
                nullSeason.Season = "0";
                nullSeason.SeasonEpisodes = new EntitySet<TraktEpisode>();
                nullSeason.SeasonEpisodes.Add(TraktEpisode.NULLEPISODE);
                nullSeason.SeasonID = 0;
                nullSeason.Tvdb = "0000";
                nullSeason.Url = "http://fail.com";

                return nullSeason;
            }
        }
    }

}

using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controllers;
using WPtraktBase.DAO;
using WPtraktBase.Model;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.Controller
{
    public class EpisodeController
    {
        private ShowDao showDao;

        public EpisodeController()
        {
            this.showDao = ShowDao.Instance;
        }

        public async Task<TraktEpisode> getEpisodeByTvdbAndSeasonInfo(String tvdbId, String season, String episode, TraktShow show)
        {
            if (show.Seasons.Count == 0)
            {
                ShowController showController = new ShowController();
                TraktSeason[] seasons = await showController.getSeasonsByTVDBID(tvdbId);
                foreach (TraktSeason traktSeason in seasons)
                    traktSeason.SeasonEpisodes = new EntitySet<TraktEpisode>();

                showController.AddSeasonsToShow(show, seasons);
            }

            return await showDao.getEpisodeByTvdbAndSeasonInfo(tvdbId, season, episode);
        }


        public async Task<Boolean> addEpisodeToWatchlist(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.addEpisodeToWatchlist(TVDBID, IMDBID, title, year, season, episode);
            }
            else
                return false;
        }

        public async Task<Boolean> removeEpisodeFromWatchlist(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.removeEpisodeFromWatchlist(TVDBID, IMDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> checkinEpisode(String TVDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.checkinEpisode(TVDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }


        public async Task<Boolean> markEpisodeAsSeen(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.markEpisodeAsSeen(TVDBID, IMDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> unMarkEpisodeAsSeen(String TVDBID, String IMDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.unMarkEpisodeAsSeen(TVDBID, IMDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }


        public async Task<Boolean> addShoutToEpisode(String shout, String TVDBID, String title, Int16 year, String season, String episode)
        {
            if (!String.IsNullOrEmpty(TVDBID) && !String.IsNullOrEmpty(shout) && !String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(season) && !String.IsNullOrEmpty(episode) && year > 0)
            {
                return await showDao.addShoutToEpisode(shout, TVDBID, title, year, season, episode);
            }
            else
            {
                return false;
            }
        }

        public async Task<TraktShout[]> getShoutsForEpisode(String TVDBID, String season, String episode)
        {
            return await showDao.getShoutsForEpisode(TVDBID, season, episode);
        }

        public Boolean updateEpisode(TraktEpisode traktEpisode)
        {
            return showDao.saveEpisode(traktEpisode);
        }

        public async void CreateTile()
        {
            try
            {
                if (IsolatedStorageFile.GetUserStoreForApplication().FileExists("upcomming.json"))
                {
                    using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream stream = isoStore.OpenFile("upcomming.json", FileMode.Open))
                        {
                            var ser = new DataContractJsonSerializer(typeof(TraktCalendar[]));
                            TraktCalendar[] cal = (TraktCalendar[])ser.ReadObject(stream);

                            if (cal.Length == 0)
                            {
                                createNoUpcommingTile();
                            }
                            else
                            {
                                TraktCalendarEpisode nextEpisode = null;

                                await DownloadRequiredWallpapers(cal);

                                if (AppUser.Instance.LiveTileType == LiveTileType.Random)
                                {
                                    nextEpisode = LookupRandomNextEpisode(cal, nextEpisode);
                                }
                                else
                                {
                                    nextEpisode = LookupNextEpisode(cal, nextEpisode);
                                }

                                if (nextEpisode != null)
                                {
                                    CreateEpisodeTile(nextEpisode);
                                }
                                else
                                {
                                    createNoUpcommingTile();
                                    UpdateUpcomming();
                                }
                            }
                        }

                    }
                }
                else
                {
                    createNoUpcommingTile();
                    UpdateUpcomming();
                }
            }
            catch (IsolatedStorageException)
            {
                createNoUpcommingTile();
            }
        }

        private async Task<Boolean> DownloadRequiredWallpapers(TraktCalendar[] cal)
        {
            foreach (TraktCalendar calendarItem in cal)
            {
                foreach(TraktCalendarEpisode calEpisode in calendarItem.Episodes)
                {
                    String fileName = calEpisode.Show.tvdb_id + "background" + ".jpg";

                    if (!StorageController.doesFileExist(fileName))
                    {
                        if (StorageController.IsConnectedToWifi())
                        {
                            await ShowController.getFanart(calEpisode.Show.tvdb_id, calEpisode.Show.Images.Fanart);
                        }
                    }
                }
            }

            return true;
        }

        private static TraktCalendarEpisode LookupRandomNextEpisode(TraktCalendar[] cal, TraktCalendarEpisode nextEpisode)
        {
            List<TraktCalendarEpisode> upcommingEpisodes = new List<TraktCalendarEpisode>();


            foreach (TraktCalendar calendar in cal)
            {
                DateTime calDate = DateTime.Parse(calendar.Date);

                if ((DateTime.UtcNow.Year <= calDate.Year) && (DateTime.UtcNow.Month <= calDate.Month) && (DateTime.UtcNow.DayOfYear <= calDate.DayOfYear))
                {
                    if ((calDate - DateTime.Now).Days > 7)
                        break;

                    foreach (TraktCalendarEpisode episode in calendar.Episodes)
                    {
                        episode.Date = calDate;
                        upcommingEpisodes.Add(episode);
                    }
                }

            }
            try
            {
                nextEpisode = upcommingEpisodes[new Random().Next(0, upcommingEpisodes.Count)];
            }
            catch (ArgumentOutOfRangeException) { return null; }
            return nextEpisode;
        }

        private static TraktCalendarEpisode LookupNextEpisode(TraktCalendar[] cal, TraktCalendarEpisode nextEpisode)
        {
            List<TraktCalendarEpisode> upcommingEpisodes = new List<TraktCalendarEpisode>();


            foreach (TraktCalendar calendar in cal)
            {
                DateTime calDate = DateTime.Parse(calendar.Date);

                if ((DateTime.UtcNow.Year <= calDate.Year) && (DateTime.UtcNow.Month <= calDate.Month) && (DateTime.UtcNow.DayOfYear <= calDate.DayOfYear))
                {
                    if ((calDate - DateTime.Now).Days > 7)
                        break;
                    Int32 next = new Random().Next(0, calendar.Episodes.Length);
                    TraktCalendarEpisode newItem = calendar.Episodes[next];
                    newItem.Date = calDate;
                    return newItem;
                }

            }

            return null;
        }

        private void CreateEpisodeTile(TraktCalendarEpisode nextEpisode)
        {
            ShellTile appTile = ShellTile.ActiveTiles.First();

            if (appTile != null)
            {


                if (StorageController.doesFileExist(nextEpisode.Show.tvdb_id + "background.jpg"))
                {
                    FlipTileData newTileData = new FlipTileData();
                    newTileData.BackContent = nextEpisode.Show.Title + ", " + nextEpisode.Episode.Season + "x" + nextEpisode.Episode.Number;
                    newTileData.BackTitle = ((nextEpisode.Date.DayOfYear == DateTime.UtcNow.DayOfYear) ? ("Today, " + nextEpisode.Episode.FirstAiredAsDate.ToShortTimeString()) : (nextEpisode.Episode.FirstAiredAsDate.ToShortDateString()));


                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        copyImageToShellContent(nextEpisode.Show.tvdb_id + "background.jpg", nextEpisode.Show.tvdb_id);

                        if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(nextEpisode.Show.tvdb_id + "background.jpg"))
                        {
                            LockscreenHelper.UpdateLockScreen("ms-appdata:///Local/" + nextEpisode.Show.tvdb_id + "background.jpg");
                        }

                        newTileData.BackgroundImage =
                           new Uri("isostore:/Shared/ShellContent/wptraktbg" + nextEpisode.Show.tvdb_id + ".jpg", UriKind.Absolute);
                        newTileData.WideBackgroundImage =
                        new Uri("isostore:/Shared/ShellContent/wptraktbg" + nextEpisode.Show.tvdb_id + ".jpg", UriKind.Absolute);
                        newTileData.SmallBackgroundImage = new Uri("isostore:/Shared/ShellContent/wptraktbg" + nextEpisode.Show.tvdb_id + ".jpg", UriKind.Absolute);
                        newTileData.WideBackContent = nextEpisode.Show.Title + ", " + nextEpisode.Episode.Season + "x" + nextEpisode.Episode.Number + "\r\n" + nextEpisode.Episode.Title + "\r\n" + ((nextEpisode.Date.DayOfYear == DateTime.UtcNow.DayOfYear) ? ("Today, " + nextEpisode.Episode.FirstAiredAsDate.ToShortTimeString()) : (nextEpisode.Episode.FirstAiredAsDate.ToShortDateString())) + " ( " + nextEpisode.Show.Network + " )";
                        appTile.Update(newTileData);
                    });


                }
                else
                {
                    FlipTileData newTileData = new FlipTileData();
                    newTileData.BackContent = nextEpisode.Show.Title + ", " + nextEpisode.Episode.Season + "x" + nextEpisode.Episode.Number;
                    newTileData.BackTitle = ((nextEpisode.Date.DayOfYear == DateTime.UtcNow.DayOfYear) ? ("Today, " + nextEpisode.Episode.FirstAiredAsDate.ToShortTimeString()) : (nextEpisode.Episode.FirstAiredAsDate.ToShortDateString()));


                    newTileData.BackgroundImage = new Uri("appdata:background.png");
                    newTileData.WideBackgroundImage = new Uri("appdata:WideBackground.png");
                    newTileData.SmallBackgroundImage = new Uri("appdata:background.png");
                    newTileData.WideBackContent = nextEpisode.Show.Title + ", " + nextEpisode.Episode.Season + "x" + nextEpisode.Episode.Number + "\r\n" + nextEpisode.Episode.Title + "\r\n" + ((nextEpisode.Date.DayOfYear == DateTime.UtcNow.DayOfYear) ? ("Today, " + nextEpisode.Episode.FirstAiredAsDate.ToShortTimeString()) : (nextEpisode.Episode.FirstAiredAsDate.ToShortDateString())) + " ( " + nextEpisode.Show.Network + " )";
                    appTile.Update(newTileData);
                }


            }
        }


        private void createNoUpcommingTile()
        {
            ShellTile appTile = ShellTile.ActiveTiles.First();

            if (appTile != null)
            {
                FlipTileData newTileData = new FlipTileData();
                newTileData.BackgroundImage = new Uri("appdata:background.png");
                newTileData.WideBackgroundImage = new Uri("appdata:WideBackground.png");
                newTileData.SmallBackgroundImage = new Uri("appdata:background.png");
                newTileData.BackContent = "No upcoming episodes";
                newTileData.WideBackContent = "No upcoming episodes";
                newTileData.BackTitle = DateTime.Now.ToShortTimeString();

                appTile.Update(newTileData);
            }
        }

        private void UpdateUpcomming()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                var calendarClient = new WebClient();
                calendarClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadCalendartringCompleted);
                calendarClient.UploadStringAsync(new Uri("https://api.trakt.tv/user/calendar/shows.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName + "/" + DateTime.Now.ToString("yyyyMMdd") + "/14"), AppUser.createJsonStringForAuthentication());
            }
        }

        void client_UploadCalendartringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    //parse into jsonser
                    var ser = new DataContractJsonSerializer(typeof(TraktCalendar[]));
                    TraktCalendar[] obj = (TraktCalendar[])ser.ReadObject(ms);

                    using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {

                        using (var isoFileStream = isoStore.CreateFile("upcomming.json"))
                        {

                            ser.WriteObject(isoFileStream, obj);

                            isoFileStream.Close();
                        }

                    }

                }
            }
            catch (WebException)
            {
            }
            catch (TargetInvocationException) { }
        }

        private void copyImageToShellContent(String filename, String uniquekey)
        {
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!StorageController.doesFileExist("/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg") && StorageController.doesFileExist(filename))
                    {
                        store.CopyFile(filename, "/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg");
                    }
                }
            }
            catch (IsolatedStorageException) { }
        }


        private void saveImageForTile(String fileName, Stream pic, Int16 width, Int16 quality)
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var bi = new BitmapImage();
                bi.SetSource(pic);

                try
                {
                    var wb = new WriteableBitmap(bi);

                    double newHeight = wb.PixelHeight * ((double)width / wb.PixelWidth);

                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        wb.SaveJpeg(isoFileStream, width, (int)newHeight, 0, quality);
                        isoFileStream.Close();
                        wb = null;
                        bi = null;
                    }
                }
                catch (IsolatedStorageException)
                { }
            }
        }
    }
}

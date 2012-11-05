using System.Windows;
using Microsoft.Phone.Scheduler;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using System;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Net.NetworkInformation;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;
using WPtrakt.Model;
using WPtrakt.Controllers;

namespace WPtraktLiveTile
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            //TODO: Add code to perform your task in background
            if (task is PeriodicTask)
            {
                CreateTile();
            }
        }

        private void CreateTile()
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

            nextEpisode = upcommingEpisodes[new Random().Next(upcommingEpisodes.Count)];
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


                    return calendar.Episodes[0];
                    
                }

            }

            return null;
        }

        private void CreateEpisodeTile(TraktCalendarEpisode nextEpisode)
        {
            ShellTile appTile = ShellTile.ActiveTiles.First();

            if (appTile != null)
            {
                StandardTileData newTileData = new StandardTileData();
                newTileData.BackContent = nextEpisode.Show.Title + ", " + nextEpisode.Episode.Season + "x" + nextEpisode.Episode.Number;
                newTileData.BackTitle = ((nextEpisode.Date.DayOfYear == DateTime.UtcNow.DayOfYear) ? ("Today," + nextEpisode.Show.AirTime) : (nextEpisode.Date.ToLocalTime().ToShortDateString()));
                if (AppUser.Instance.LiveTileUsePoster)
                {
                    if (StorageController.doesFileExist(nextEpisode.Show.tvdb_id + "largebackground.jpg"))
                    {
                         Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ImageController.copyImageToShellContent(nextEpisode.Show.tvdb_id + "largebackground.jpg", nextEpisode.Show.tvdb_id);
                            newTileData.BackgroundImage = new Uri("isostore:/Shared/ShellContent/wptraktbg" + nextEpisode.Show.tvdb_id + ".jpg", UriKind.Absolute);
                            appTile.Update(newTileData);
                            NotifyComplete();
                        }));         
                    }
                    else
                    {
                        this.LastTvDB = nextEpisode.Show.tvdb_id;
                        newTileData.BackgroundImage = new Uri("appdata:background.png");
                        appTile.Update(newTileData);
                        HttpWebRequest request;

                        request = (HttpWebRequest)WebRequest.Create(new Uri(nextEpisode.Show.Images.Fanart));
                        request.BeginGetResponse(new AsyncCallback(request_OpenReadFanartCompleted), new object[] { request });
                    }
                }
                else
                {
                    newTileData.BackgroundImage = new Uri("appdata:background.png");
                    appTile.Update(newTileData);
                    NotifyComplete();
                }

               
              
            }
        }

        private String LastTvDB { get; set; }

        void request_OpenReadFanartCompleted(IAsyncResult r)
        {
            try
            {
                object[] param = (object[])r.AsyncState;
                HttpWebRequest httpRequest = (HttpWebRequest)param[0];

                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                System.Net.HttpStatusCode status = httpResoponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    Stream str = httpResoponse.GetResponseStream();

                    Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ImageController.saveImageForTile(LastTvDB + "largebackground.jpg", str, 1920, 100);
                    }));
                }
            }
            catch (WebException) { }

            CreateTile();
        }

        private void createNoUpcommingTile()
        {
            ShellTile appTile = ShellTile.ActiveTiles.First();

            if (appTile != null)
            {
                StandardTileData newTileData = new StandardTileData();
                newTileData.BackgroundImage = new Uri("appdata:background.png");
                newTileData.BackContent = "No upcomming episodes";
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
                calendarClient.UploadStringAsync(new Uri("http://api.trakt.tv/user/calendar/shows.json/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName + "/" + DateTime.Now.ToString("yyyyMMdd") + "/14"), AppUser.createJsonStringForAuthentication());
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
                NotifyComplete();
            }
            catch (WebException)
            {
            }
        }
    }
}
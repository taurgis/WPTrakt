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
using WPtraktBase.Model;
using System.Reflection;
using Windows.Phone.PersonalInformation;
using System.Threading.Tasks;
using Windows.Phone.SocialInformation;
using WPtraktBase.Controller;
using Windows.Web.Http;
using Windows.Storage.Streams;

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
        private ContactBindingManager contactBindingManager;
        async protected override void OnInvoke(ScheduledTask task)
        {
            //TODO: Add code to perform your task in background
            if (task is PeriodicTask && !task.Name.Equals("ExtensibilityTaskAgent"))
            {
                CreateTile();
            }
            else
            { 
                this.contactBindingManager = await ContactBindings.GetAppContactBindingManagerAsync();

                // Use the name of the task to differentiate between the ExtensilityTaskAgent 
                // and the ScheduledTaskAgent
                if (task.Name == "ExtensibilityTaskAgent")
                {
                    List<Task> inprogressOperations = new List<Task>();

                    OperationQueue operationQueue = await SocialManager.GetOperationQueueAsync();
                    ISocialOperation socialOperation = await operationQueue.GetNextOperationAsync();

                    while (null != socialOperation)
                    {


                        try
                        {
                            switch (socialOperation.Type)
                            {
                                case SocialOperationType.DownloadRichConnectData:
                                    await ProcessOperationAsync(socialOperation as DownloadRichConnectDataOperation);
                                    break;


                            }

                            socialOperation = await operationQueue.GetNextOperationAsync();
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }
        }

        private async Task ProcessOperationAsync(DownloadRichConnectDataOperation operation)
        {
           try
            {
                await ParallelForEach(operation.Ids, async (string remoteId) =>
                {
                    if (remoteId.Contains(","))
                        remoteId = remoteId.Split(',')[0];


                    ContactBinding binding = await contactBindingManager.GetContactBindingByRemoteIdAsync(remoteId);
                    TraktProfile tileData = await new UserController().GetUserProfile(remoteId);

                    binding.TileData = new ConnectTileData();
                    binding.TileData.Title = tileData.Username;
                 
                    ConnectTileImage tileImage = new ConnectTileImage();
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(tileData.Avatar));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;
                    MemoryStream memoryStream = new MemoryStream();
                    webResponse.GetResponseStream().CopyTo(memoryStream);
                    IRandomAccessStream stream = await ConvertToRandomAccessStream(memoryStream);
                    await tileImage.SetImageAsync(stream);

                    binding.TileData.Images.Add(tileImage);

                    await contactBindingManager.SaveContactBindingAsync(binding);
                });
            }
            catch (Exception e)
            {
              
            }
            finally
            {

                NotifyComplete();
            }
        }

        public static async Task<IRandomAccessStream> ConvertToRandomAccessStream(MemoryStream memoryStream)
        {
            var randomAccessStream = new InMemoryRandomAccessStream();
            var outputStream = randomAccessStream.GetOutputStreamAt(0);
            var dw = new DataWriter(outputStream);
            var task = Task.Factory.StartNew(() => dw.WriteBytes(memoryStream.ToArray()));
            await task;
            await dw.StoreAsync();
            await outputStream.FlushAsync();
            return randomAccessStream;
        }

        public static async Task ParallelForEach<TSource>(IEnumerable<TSource> collection, Func<TSource, Task> work, uint maxTasksRunningInParallel = 6)
        {
            List<Task> inprogressTasks = new List<Task>();
            foreach (TSource item in collection)
            {
                // limit the number of simultaneous tasks
                if (inprogressTasks.Count >= maxTasksRunningInParallel)
                {
                    Task completed = await Task.WhenAny(inprogressTasks);
                    inprogressTasks.Remove(completed);
                }
                inprogressTasks.Add(work(item));
            }

            // wait for all the tasks to complete
            if (inprogressTasks.Count > 0)
            {
                await Task.WhenAll(inprogressTasks);
                inprogressTasks.Clear();
            }
        }

        private void CreateTile()
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


                if (doesFileExist(nextEpisode.Show.tvdb_id + "background.jpg"))
                {
                FlipTileData newTileData = new FlipTileData();
                newTileData.BackContent = nextEpisode.Show.Title + ", " + nextEpisode.Episode.Season + "x" + nextEpisode.Episode.Number;
                newTileData.BackTitle = ((nextEpisode.Date.DayOfYear == DateTime.UtcNow.DayOfYear) ? ("Today, " + nextEpisode.Episode.FirstAiredAsDate.ToShortTimeString()) : (nextEpisode.Episode.FirstAiredAsDate.ToShortDateString()));
               
               
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        copyImageToShellContent(nextEpisode.Show.tvdb_id + "background.jpg", nextEpisode.Show.tvdb_id);
                        LockscreenHelper.UpdateLockScreen("ms-appdata:///Local/" + nextEpisode.Show.tvdb_id + "background.jpg");
                        newTileData.BackgroundImage =
                           new Uri("isostore:/Shared/ShellContent/wptraktbg" + nextEpisode.Show.tvdb_id + ".jpg", UriKind.Absolute);
                        newTileData.WideBackgroundImage =
                        new Uri("isostore:/Shared/ShellContent/wptraktbg" + nextEpisode.Show.tvdb_id + ".jpg", UriKind.Absolute);
                        newTileData.SmallBackgroundImage = new Uri("isostore:/Shared/ShellContent/wptraktbg" + nextEpisode.Show.tvdb_id + ".jpg", UriKind.Absolute);
                        newTileData.WideBackContent = nextEpisode.Show.Title + ", " + nextEpisode.Episode.Season + "x" + nextEpisode.Episode.Number + "\r\n" + nextEpisode.Episode.Title + "\r\n" + ((nextEpisode.Date.DayOfYear == DateTime.UtcNow.DayOfYear) ? ("Today, " + nextEpisode.Episode.FirstAiredAsDate.ToShortTimeString()) : (nextEpisode.Episode.FirstAiredAsDate.ToShortDateString())) + " ( " + nextEpisode.Show.Network + " )";
                        appTile.Update(newTileData);

                        NotifyComplete();
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


                    NotifyComplete();
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
                NotifyComplete();
            }
            catch (WebException)
            {
                //DO NOTHING
            }
            catch (TargetInvocationException)
            {
                //DO NOTHING
            }
        }

        private void copyImageToShellContent(String filename, String uniquekey)
        {
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!doesFileExist("/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg") && doesFileExist(filename))
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

        private  Boolean doesFileExist(String filename)
        {
            try
            {
                return IsolatedStorageFile.GetUserStoreForApplication().FileExists(filename);
            }
            catch (IsolatedStorageException)
            {
                return false;
            }
        }
    }
}
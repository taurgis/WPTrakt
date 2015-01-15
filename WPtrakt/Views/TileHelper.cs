using Microsoft.Phone.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPtrakt.Model;
using WPtraktBase.Controller;

namespace WPtrakt.Views
{
    public class TileHelper
    {
        public static async void StartReloadLiveTile()
        {
            await ReloadLiveTile();
        }

        private static Task<Boolean> ReloadLiveTile()
        {
            var tcs = new TaskCompletionSource<Boolean>();
            try
            {
                if (AppUser.Instance.LiveTileEnabled)
                {
                    App.TrackEvent("MainPage", "Refresh live tile");

                    var taskName = "WPtraktLiveTile";

                    // If the task exists
                    var oldTask = ScheduledActionService.Find(taskName) as PeriodicTask;
                    if (oldTask != null)
                    {
                        ScheduledActionService.Remove(taskName);
                    }

                    // Create the Task
                    PeriodicTask task = new PeriodicTask(taskName);

                    // Description is required
                    task.Description = "This task updates the WPtrakt live tile.";

                    // Add it to the service to execute
                    ScheduledActionService.Add(task);
                    //ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(3));
                    new EpisodeController().CreateTile();
                    tcs.TrySetResult(true);

                }
            }
            catch (InvalidOperationException) { tcs.TrySetResult(false); }

            return tcs.Task;
        }
    }
}

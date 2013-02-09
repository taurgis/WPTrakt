using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.Model.Trakt.Request;

namespace WPtrakt
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
            DataContext = App.SettingsViewModel;
           
            this.Loaded += new RoutedEventHandler(SettingsPage_Loaded);
            
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppUser.Instance.LiveTileEnabled)
            {
                this.toggle.IsChecked = true;
                this.toggleRandom.IsEnabled = true;
                this.togglePoster.IsEnabled = true;
                if (AppUser.Instance.LiveTileType == LiveTileType.Random)
                {
                    toggleRandom.IsChecked = true;
                    this.toggleRandom.Content = "Enabled";
                }
                else
                {
                    toggleRandom.IsChecked = false;
                    this.toggleRandom.Content = "Disabled";
                }

                if (AppUser.Instance.LiveTileUsePoster)
                {
                    togglePoster.IsChecked = true;
                    this.togglePoster.Content = "Enabled";
                }
                else
                    this.togglePoster.Content = "Disabled";
            }
            else
            {
                this.toggle.IsChecked = false;
                this.toggleRandom.IsEnabled = false;
                this.togglePoster.IsEnabled = false;
            }

            if (!App.SettingsViewModel.IsDataLoaded)
            {
                App.SettingsViewModel.LoadData();

                if (AppUser.Instance.BackgroundWallpapersEnabled)
                {
                    this.toggleWallpaper.IsChecked = true;
                    this.toggleWallpaper.Content = "Enabled";
                }
                else
                {
                    this.toggleWallpaper.IsChecked = false;
                    this.toggleWallpaper.Content = "Disabled";
                }

                if (AppUser.Instance.SmallScreenshotsEnabled)
                {
                    this.toggleSmallScreens.IsChecked = true;
                    this.toggleSmallScreens.Content = "Enabled";
                }
                else
                {
                    this.toggleSmallScreens.IsChecked = false;
                    this.toggleSmallScreens.Content = "Disabled";
                }


                if (AppUser.Instance.ImagesWithWIFI)
                {
                    this.toggleWifi.IsChecked = true;
                    this.toggleWifi.Content = "Enabled";
                }
                else
                {
                    this.toggleWifi.IsChecked = false;
                    this.toggleWifi.Content = "Disabled";
                }

                App.SettingsViewModel.Usage = "Calculating...";
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                worker.RunWorkerAsync();


                string lockscreenValue = "";

                bool lockscreenValueExists = NavigationContext.QueryString.TryGetValue("lockscreen", out lockscreenValue);
                if (lockscreenValueExists)
                    this.SettingsPanorama.DefaultItem = this.SettingsPanorama.Items[3];

 
                Animation.ControlFadeInSlow(this.LayoutRoot);
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
            long usage = 0;

            foreach (String file in myIsolatedStorage.GetFileNames())
            {
                try
                {
                    IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile(file, FileMode.Open);
                    usage += stream.Length;
                    stream.Close();
                }
                catch (IsolatedStorageException) { }
            }

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                App.SettingsViewModel.Usage = (((usage / 1024))).ToString() + " kB";
                ClearCacheButton.IsEnabled = true;
            });
         
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            AppUser.ClearCache();
     
            App.SettingsViewModel.Usage = "Cleared";
            App.SettingsViewModel.NotifyPropertyChanged("Usage");
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            GoBack();
        }

        private void GoBack()
        {
            if (toggle.IsChecked == true)
            {
                try
                {
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
                }
                catch (InvalidOperationException) { }

                AppUser.Instance.LiveTileEnabled = true;
                AppUser.Instance.LiveTileUsePoster = false;

                if ((Boolean)toggleRandom.IsChecked)
                {
                    AppUser.Instance.LiveTileType = LiveTileType.Random;
                }
                else
                {
                    AppUser.Instance.LiveTileType = LiveTileType.ByDate;
                }
            }
            else
            {
                AppUser.Instance.LiveTileEnabled = false;
                DisableLiveTile();
            }

            AppUser.Instance.SmallScreenshotsEnabled = (Boolean)toggleSmallScreens.IsChecked;
            AppUser.Instance.BackgroundWallpapersEnabled = (Boolean)toggleWallpaper.IsChecked;
            AppUser.Instance.ImagesWithWIFI = (Boolean)toggleWifi.IsChecked;
        }

        private void DisableLiveTile()
        {
            var taskName = "WPtraktLiveTile";
            try
            {
                ScheduledActionService.Remove(taskName);
            }
            catch (InvalidOperationException) { }
            updateTileToStandard();
        }

        private void TextBlock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask emailcomposer = new EmailComposeTask();

            emailcomposer.To = "wptrakt@outlook.com";
            emailcomposer.Subject = "WPTrakt Support Question/Bugreport";
            emailcomposer.Body = "";
            emailcomposer.Show();
        }

        private void toggle_Checked(object sender, RoutedEventArgs e)
        {
            this.toggle.Content = "Enabled";
            this.toggleRandom.IsEnabled = true;
            this.togglePoster.IsEnabled = true;
        }

        private void toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.toggle.Content = "Disabled";
            this.toggleRandom.IsEnabled = false;
            this.toggleRandom.IsChecked = false;
            this.toggleRandom.Content = "Disabled";
            this.togglePoster.IsEnabled = false;
            this.togglePoster.IsChecked = false;
            this.togglePoster.Content = "Disabled";
        }

        private void updateTileToStandard()
        {
            ShellTile appTile = ShellTile.ActiveTiles.First();

            if (appTile != null)
            {
                StandardTileData newTileData = new StandardTileData();
                newTileData.BackgroundImage = new Uri("appdata:background.png");
                newTileData.BackContent = "";
                newTileData.BackTitle = "";
                
                appTile.Update(newTileData);
            }
        }


        private void toggleRandom_Checked_1(object sender, RoutedEventArgs e)
        {
            toggleRandom.Content = "Enabled";
        }

        private void toggleRandom_Unchecked_1(object sender, RoutedEventArgs e)
        {
            toggleRandom.Content = "Disabled";
        }

        

        private void Twitter_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("https://twitter.com/theunenth");

            task.Show();
        }
        private void toggleWallpaper_Checked_1(object sender, RoutedEventArgs e)
        {
            this.toggleWallpaper.Content = "Enabled";

        }

        private void toggleWallpaper_Unchecked_1(object sender, RoutedEventArgs e)
        {
            this.toggleWallpaper.Content = "Disabled";

        }

        private void toggleSmallScreens_Checked_1(object sender, RoutedEventArgs e)
        {
            this.toggleSmallScreens.Content = "Enabled";

        }

        private void toggleSmallScreens_Unchecked_1(object sender, RoutedEventArgs e)
        {
            this.toggleSmallScreens.Content = "Disabled";
     
        }

        private void toggleWifi_Checked_1(object sender, RoutedEventArgs e)
        {
            this.toggleWifi.Content = "Enabled";
        }

        private void toggleWifi_Unchecked_1(object sender, RoutedEventArgs e)
        {
            this.toggleWifi.Content = "Disabled";
        }
    }
}
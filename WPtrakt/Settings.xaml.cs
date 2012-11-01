using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using VPtrakt.Controllers;
using VPtrakt.Model.Trakt.Request;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;

namespace WPtrakt
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();

            DataContext = App.SettingsViewModel;
           
            this.Loaded += new RoutedEventHandler(SettingsPage_Loaded);
            var taskName = "WPtraktLiveTile";
            var oldTask = ScheduledActionService.Find(taskName) as PeriodicTask;
            if (oldTask != null)
            {
                this.toggle.IsChecked = true;
            }
            else
                this.toggle.IsChecked = false;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.SettingsViewModel.IsDataLoaded)
            {
                App.SettingsViewModel.LoadData();
               
            }
            App.SettingsViewModel.Usage = "Calculating...";
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {

                IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
                long usage = 0;

                foreach (String file in myIsolatedStorage.GetFileNames())
                {
                    IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile(file, FileMode.Open);
                    usage += stream.Length;
                    stream.Close();
                }

                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.SettingsViewModel.Usage = (((usage / 1024))).ToString() + " kB";
                });
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            String tempUsername = AppUser.Instance.UserName;
            String tempPassword = AppUser.Instance.Password;

            IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();

            foreach (String file in myIsolatedStorage.GetFileNames())
            {
               myIsolatedStorage.DeleteFile(file);
            }

            IsolatedStorageSettings.ApplicationSettings["UserName"] = tempUsername;
            IsolatedStorageSettings.ApplicationSettings["Password"] = tempPassword;
            IsolatedStorageSettings.ApplicationSettings.Save();

            foreach(String dir in myIsolatedStorage.GetDirectoryNames())
            {
                if (!dir.Contains("Shared"))
                {
                    foreach (String file in myIsolatedStorage.GetFileNames(dir + "/*"))
                    {
                        myIsolatedStorage.DeleteFile(dir + "/" + file);
                    }
                }
            }
      

            App.SettingsViewModel.Usage = "Cleared";
            App.SettingsViewModel.NotifyPropertyChanged("Usage");
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (toggle.IsChecked == true)
            {
                var taskName = "WPtraktLiveTile";
                PeriodicTask task = new PeriodicTask(taskName);
                task.Description = "This task updates the WPtrakt live tile.";
                //at this point there are no tasks in background tasks of phone settings
                try
                {
                    ScheduledActionService.Add(task);
                }
                catch (InvalidOperationException) { }
                //at this point, there are two tasks with app title, same description
                //ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(3));
            }
            else
            {
                DisableLiveTile();
            }
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
        }

        private void toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.toggle.Content = "Disabled";
           
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

        private void TextBlock_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            CreateButton.Visibility = System.Windows.Visibility.Visible;
            EmailText.Visibility = System.Windows.Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            btnCreate.IsEnabled = false;
            btnCreate.Content = "Creating..";
            string MatchEmailPattern =
            @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
     + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
     + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
     + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

            if (String.IsNullOrEmpty(AppUser.Instance.UserName) || String.IsNullOrEmpty(AppUser.Instance.Password) || String.IsNullOrEmpty(txtEmail.Text))
            {
                MessageBox.Show("Please fill in all fields");
                return;
            }

            if (Regex.IsMatch(txtEmail.Text, MatchEmailPattern))
            {
                progressBar.Visibility = System.Windows.Visibility.Visible;
                var registerClient = new WebClient();
                registerClient.UploadStringCompleted += new UploadStringCompletedEventHandler(registerClient_UploadStringCompleted);

                RegisterAuth auth = new RegisterAuth();
                auth.Email = txtEmail.Text;
                registerClient.UploadStringAsync(new Uri("http://api.trakt.tv/account/create/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(RegisterAuth), auth));
            }
            else
                MessageBox.Show("Invalid email!");
        }

        void registerClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            btnCreate.IsEnabled = true;
            btnCreate.Content = "Create";
            try
            {
                String jsonString = e.Result;
                if (jsonString.Contains(" is already a registered e-mail."))
                {
                    MessageBox.Show("Email already in use.");
                    return;
                }

                if (jsonString.Contains("is already a registered username"))
                {
                    MessageBox.Show("Username already in use.");
                    return;
                }

                if (jsonString.Contains("created account for"))
                {
                    MessageBox.Show("Successfully created an account! It could take up to a minute for your account to work.");
                    try
                    {
                        IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json");
                    }
                    catch (IsolatedStorageException) { }
                    App.ViewModel.Profile = null;
                    NavigationService.GoBack();
                    App.ViewModel.LoadData();
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using System.IO.IsolatedStorage;
using WPtrakt.Model;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.IO;
using VPtrakt.Model.Trakt.Request;
using VPtrakt.Controllers;
using System.Text.RegularExpressions;
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
                    if (file.EndsWith(".jpg"))
                    {
                        IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile(file, FileMode.Open);
                        usage += stream.Length;
                        stream.Close();
                    }
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
                if (file.EndsWith(".jpg"))
                {
                    myIsolatedStorage.DeleteFile(file);
                }
            }
            IsolatedStorageSettings.ApplicationSettings["UserName"] = tempUsername;
            IsolatedStorageSettings.ApplicationSettings["Password"] = tempPassword;
            IsolatedStorageSettings.ApplicationSettings.Save();

            App.SettingsViewModel.Usage = "Cleared";
            App.SettingsViewModel.NotifyPropertyChanged("Usage");
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.SettingsPanorama.DefaultItem = this.SettingsPanorama.Items[0];
            progressBar.Visibility = System.Windows.Visibility.Visible;
            var profileClient = new WebClient();

            profileClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_DownloadProfileStringCompleted);
            profileClient.UploadStringAsync(new Uri("http://api.trakt.tv/account/test/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
        }

        void client_DownloadProfileStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            progressBar.Visibility = System.Windows.Visibility.Collapsed;
            try
            {
                String jsonString = e.Result;

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

                try
                {
                    NavigationService.GoBack();
                }
                catch (InvalidOperationException) { }
               
            }
            catch (WebException)
            {
                DisableLiveTile();
                if (MessageBox.Show("Invalid login data. Press cancel to exit.", "Error", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    throw new Exception("ExitAppException");
                }
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
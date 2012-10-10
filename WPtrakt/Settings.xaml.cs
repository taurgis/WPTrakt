using System;
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

namespace WPtrakt
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();

            DataContext = App.SettingsViewModel;
           
            this.Loaded += new RoutedEventHandler(SettingsPage_Loaded);
            this.toggle.IsEnabled = false;  
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.SettingsViewModel.IsDataLoaded)
            {
                App.SettingsViewModel.LoadData();
            }
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            IsolatedStorageFile.GetUserStoreForApplication().Remove();
            App.SettingsViewModel.NotifyPropertyChanged("Usage");
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.SettingsPanorama.DefaultItem = this.SettingsPanorama.Items[0];
            progressBar.Visibility = System.Windows.Visibility.Visible;
            var profileClient = new WebClient();

            profileClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_DownloadProfileStringCompleted);
            profileClient.UploadStringAsync(new Uri("http://api.trakt.tv/account/test/5eaaacc7a64121f92b15acf5ab4d9a0b/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
            
        }

        void client_DownloadProfileStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            progressBar.Visibility = System.Windows.Visibility.Collapsed;
            try
            {
                String jsonString = e.Result;

                NavigationService.GoBack();
               
            }
            catch (WebException)
            {
                if (MessageBox.Show("Invalid login data. Press cancel to exit.", "Error", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    throw new Exception("ExitAppException");
                }
            }
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
    }
}
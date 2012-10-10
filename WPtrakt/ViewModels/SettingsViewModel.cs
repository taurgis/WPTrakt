using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using WPtrakt.Model.Trakt;
using System.Windows.Threading;
using System.Threading;
using System.IO.IsolatedStorage;
using WPtrakt.Model;
using Microsoft.Phone.Info;


namespace WPtrakt
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
       

        public SettingsViewModel()
        {
         

        }

        public void LoadData()
        {
            this.IsDataLoaded = true;
        }

        public String Usage
        {
            get
            {
                IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();
                long usage = 0;

                foreach (String file in myIsolatedStorage.GetFileNames())
                {
                   IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile(file, FileMode.Open);
                   usage += stream.Length;
                   stream.Close();
                }

                return (((usage / 1024))).ToString() + " kB";
            }
        }

        public String TimeUntillNextLiveUpdate
        {
        get{
            //  int hoursSinceLastCall = (DateTime.Now - AppUser.Instance.LastExcecutedLiveTileUpdate).Hours;

           // return "Next live tile update: " + (((4 - hoursSinceLastCall ) > 0) ? 4 - hoursSinceLastCall + " Hours." : "Under 60 minutes");
            return "";
            }
        }

        public String DebugInfo
        {
            get
            {

                long applicationCurrentMemoryUsage = (long)DeviceExtendedProperties.GetValue("ApplicationCurrentMemoryUsage") / (1024 * 1024);
                long applicationPeakMemoryUsage = ((long)DeviceExtendedProperties.GetValue("ApplicationPeakMemoryUsage")) / (1024*1024);
                return "Current Memory Usage: " + applicationCurrentMemoryUsage + " MB\n Peak memory Usage: " + applicationPeakMemoryUsage + " MB";
            }
        }

        private String _userName;
        public String UserName
        {
            get
            {
                if (!String.IsNullOrEmpty(AppUser.Instance.UserName))
                    return AppUser.Instance.UserName;
                else
                    return "";
            }
            set
            {
                _userName = value;
                AppUser.Instance.UserName = value;
                NotifyPropertyChanged("UserName");
            }
        }

        private String _password;
        public String Password
        {
            get
            {
                if (!String.IsNullOrEmpty( AppUser.Instance.Password) )
                    return AppUser.Instance.Password;
                else
                    return "";
            }
            set
            {
                this._password = value;
                AppUser.Instance.Password = value;
                NotifyPropertyChanged("Password");
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
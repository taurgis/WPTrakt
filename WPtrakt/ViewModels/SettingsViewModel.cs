﻿using System;
using System.ComponentModel;
using Microsoft.Phone.Info;
using WPtrakt.Model;
using System.Reflection;


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

        public String ShowCreateAccount
        {

            get
            {

                return "Visible";

            }
        }

        private String _usage;
        public String Usage
        {
            get
            {
                return _usage;
            }

            set
            {
                _usage = value;
                NotifyPropertyChanged("Usage");
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

        public String Version
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly().FullName;
                var fullVersionNumber = assembly.Split('=')[1].Split(',')[0];
                return fullVersionNumber;
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

         

        private String _email;
        public String Email
        {
            get
            {

                return _email;
            }
            set
            {
                _email = value;
                NotifyPropertyChanged("_email");
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
             set;
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
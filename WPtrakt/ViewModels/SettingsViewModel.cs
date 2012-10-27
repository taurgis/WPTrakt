using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Info;
using WPtrakt.Model;


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
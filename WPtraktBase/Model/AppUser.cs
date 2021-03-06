using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using WPtrakt.Model.Trakt.Request;
using WPtraktBase.DAO;

namespace WPtrakt.Model
{
    public class AppUser
    {
        private static AppUser appUser;
        private IsolatedStorageSettings settings;
        public static AppUser Instance
        {
            get
            {
                if (appUser == null)
                    appUser = new AppUser();
                return appUser;
            }
        }

        private AppUser()
        {
            settings = IsolatedStorageSettings.ApplicationSettings;
        }

        public String AppVersion
        {
            get
            {

                if (settings.Contains("AppVersion"))
                    return settings["AppVersion"].ToString();
                else
                    return "";
            }
            set
            {
                settings["AppVersion"] = value;
                settings.Save();
            }
        }

        public List<String> Friends { get; set; }

        public DateTime LastUpdateLockScreen
        {
            get
            {
                if (settings.Contains("LastUpdateLockScreen"))
                    return DateTime.Parse(settings["LastUpdateLockScreen"].ToString());
                else
                    return DateTime.Now.Subtract(new TimeSpan(3, 0, 0, 0, 0));
            }
            set
            {
                settings["LastUpdateLockScreen"] = value.ToString();
                settings.Save();
            }
        }

        public Boolean BackgroundWallpapersEnabled
        {
            get
            {

                if (settings.Contains("BackgroundWallpapers"))
                    return (Boolean)settings["BackgroundWallpapers"];
                else
                    return true;
            }
            set
            {
                settings["BackgroundWallpapers"] = value;
                settings.Save();
            }
        }

        public Boolean ImagesWithWIFI
        {
            get
            {

                if (settings.Contains("ImagesWithWIFI"))
                    return (Boolean)settings["ImagesWithWIFI"];
                else
                    return true;
            }
            set
            {
                settings["ImagesWithWIFI"] = value;
                settings.Save();
            }
        }


        public Boolean SmallScreenshotsEnabled
        {
            get
            {

                if (settings.Contains("SmallScreenshots"))
                    return (Boolean)settings["SmallScreenshots"];
                else
                    return true;
            }
            set
            {
                settings["SmallScreenshots"] = value;
                settings.Save();
            }
        }

        public Boolean LiveTileEnabled
        {
            get
            {

                if (settings.Contains("LiveTileEnabled"))
                    return (Boolean)settings["LiveTileEnabled"];
                else
                    return false;
            }
            set
            {
                settings["LiveTileEnabled"] = value;
                settings.Save();
            }
        }

        public Int32 MyMoviesFilter
        {
            get
            {

                if (settings.Contains("MyMoviesFilter"))
                    return (Int32)settings["MyMoviesFilter"];
                else
                    return 0;
            }
            set
            {
                settings["MyMoviesFilter"] = value;
                settings.Save();
            }
        }

        public Int32 MyShowsFilter
        {
            get
            {

                if (settings.Contains("MyShowsFilter"))
                    return (Int32)settings["MyShowsFilter"];
                else
                    return 0;
            }
            set
            {
                settings["MyShowsFilter"] = value;
                settings.Save();
            }
        }


        public String UserName
        {
            get
            {

                if (settings.Contains("UserName"))
                    return settings["UserName"].ToString();
                else
                    return "";
            }
            set
            {
                settings["UserName"] = value;
                settings.Save();
            }
        }

        public LiveTileType LiveTileType
        {
            get
            {
                if (settings.Contains("LiveTileType"))
                    return (LiveTileType)settings["LiveTileType"];
                else
                    return LiveTileType.Random;
            }
            set
            {
                settings["LiveTileType"] = value;
                settings.Save();
            }
        }

        public int LiveWallpaperSchedule
        {
            get
            {
                if (settings.Contains("LiveWallpaperSchedule"))
                    return (int)settings["LiveWallpaperSchedule"];
                else
                    return 1;
            }
            set
            {
                settings["LiveWallpaperSchedule"] = value;
                settings.Save();
            }
        }

        private string _oldPassword;
        public String Password
        {
            get
            {

                if (settings.Contains("Password"))
                {
                    _oldPassword = settings["Password"].ToString();
                    return settings["Password"].ToString();
                }
                else
                    return "";

            }
            set
            {
                if (value != _oldPassword)
                {
                    if (String.IsNullOrEmpty(value))
                    {
                        settings["Password"] = "";
                        settings.Save();
                    }
                    else
                    {
                        String newPassword = ShaPassword(value);
                        settings["Password"] = newPassword;
                        settings.Save();
                    }
                }
            }
        }

        internal static string ShaPassword(string value)
        {
            SHA1Managed s = new SHA1Managed();
            UTF8Encoding enc = new UTF8Encoding();
            s.ComputeHash(enc.GetBytes(value));
            String newPassword = BitConverter.ToString(s.Hash).Replace("-", "");
            return newPassword;
        }

        public static String createJsonStringForAuthentication()
        {

            //Create User object.
            TraktRequestAuth user = new BasicAuth();

            user.Username = AppUser.Instance.UserName;

            user.Password = AppUser.Instance.Password;
            //Create a stream to serialize the object to.
            MemoryStream ms = new MemoryStream();

            // Serializer the User object to the stream.
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BasicAuth));
            ser.WriteObject(ms, user);
            byte[] json = ms.ToArray();
            ms.Close();
            String jsonString = Encoding.UTF8.GetString(json, 0, json.Length);
            return jsonString;

        }

        public static String createJsonStringForAuthentication(Type type, TraktRequestAuth auth)
        {
            auth.Username = AppUser.Instance.UserName;
            auth.Password = AppUser.Instance.Password;
            //Create a stream to serialize the object to.
            MemoryStream ms = new MemoryStream();

            // Serializer the User object to the stream.
            DataContractJsonSerializer ser = new DataContractJsonSerializer(type);
            ser.WriteObject(ms, auth);
            byte[] json = ms.ToArray();
            ms.Close();
            String jsonString = Encoding.UTF8.GetString(json, 0, json.Length);
            return jsonString;

        }

        public static String getReleaseDate()
        {
            return "15/11/2012";
        }


        public static void ClearCache()
        {
            String tempUsername = AppUser.Instance.UserName;
            String tempPassword = AppUser.Instance.Password;

            IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication();

            foreach (String file in myIsolatedStorage.GetFileNames())
            {

                try
                {
                    if (file.Equals("wptrakt.sdf"))
                    {
                        if (MovieDao.Instance.DatabaseExists())
                        {
                            try
                            {
                                MovieDao.Instance.DeleteDatabase();
                                MovieDao.Instance.Dispose();
                                MovieDao.DisposeDB();
                            }
                            catch (IOException) { }
                        }

                        if (ShowDao.Instance.DatabaseExists())
                        {
                            try
                            {
                                ShowDao.Instance.DeleteDatabase();
                                ShowDao.Instance.Dispose();
                                ShowDao.DisposeDB();
                            }
                            catch (IOException) { }
                        }
                    }

                    myIsolatedStorage.DeleteFile(file);

                }
                catch (IsolatedStorageException) { };

            }

            IsolatedStorageSettings.ApplicationSettings["UserName"] = tempUsername;
            IsolatedStorageSettings.ApplicationSettings["Password"] = tempPassword;
            IsolatedStorageSettings.ApplicationSettings.Save();

            foreach (String dir in myIsolatedStorage.GetDirectoryNames())
            {
                if (!dir.Contains("Shared"))
                {
                    foreach (String file in myIsolatedStorage.GetFileNames(dir + "/*"))
                    {

                        try
                        {
                            myIsolatedStorage.DeleteFile(dir + "/" + file);
                        }
                        catch (IsolatedStorageException) { };

                    }
                }
            }
        }
    }
}
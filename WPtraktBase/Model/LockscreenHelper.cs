﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPtrakt.Model;

namespace WPtraktBase.Model
{
    public class LockscreenHelper
    {
        public static Boolean IsProvider()
        {
            return Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
        }

        public static void UpdateLockScreen(string filePathOfTheImage)
        {
            try
            {
                var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;

                if (isProvider)
                {
                    var uri = new Uri(filePathOfTheImage, UriKind.Absolute);

                    switch (AppUser.Instance.LiveWallpaperSchedule)
                    {
                        case 0:
                            Windows.Phone.System.UserProfile.LockScreen.SetImageUri(uri);
                            break;
                        case 1:
                            Int32 hoursSinceLastUpdate = (DateTime.Now - AppUser.Instance.LastUpdateLockScreen).Hours;
                            if (hoursSinceLastUpdate > 5)
                            {
                                AppUser.Instance.LastUpdateLockScreen = DateTime.Now;
                                Windows.Phone.System.UserProfile.LockScreen.SetImageUri(uri);
                            }
                            break;
                        case 2:
                            Int32 daysSinceLastUpdate = (DateTime.Now - AppUser.Instance.LastUpdateLockScreen).Days;
                            if (daysSinceLastUpdate > 0)
                            {
                                AppUser.Instance.LastUpdateLockScreen = DateTime.Now;
                                Windows.Phone.System.UserProfile.LockScreen.SetImageUri(uri);
                            }
                            break;
                    }
                }
                else
                {
                    AppUser.Instance.LastUpdateLockScreen = DateTime.Now.Subtract(new TimeSpan(10,0,0,0,0));
                }
            }
            catch (System.Exception ex)
            {
                Console.Write(ex);
               //Do Nothing
            }
        }

    }
}

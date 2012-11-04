﻿using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace WPtrakt.Controllers
{
    public class Animation
    {
        public static void NavigateToFadeOut(PhoneApplicationPage page, UIElement targetElement, Uri targetPage)
        {
            try
            {
                Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
                Storyboard.SetTarget(storyboard, targetElement);
                EventHandler completedHandlerMainPage = delegate { };

                completedHandlerMainPage = delegate
                {
                    page.NavigationService.Navigate(targetPage);
                    storyboard.Completed -= completedHandlerMainPage;
                    storyboard.Stop();
                    targetElement.Opacity = 0;
                };

                storyboard.Completed += completedHandlerMainPage;
                storyboard.Begin();
            }
            catch (InvalidOperationException) { }
        }


        public static void FadeOut(UIElement targetElement)
        {
            try
            {
                Storyboard storyboard = Application.Current.Resources["FadeOut"] as Storyboard;
                Storyboard.SetTarget(storyboard, targetElement);
                EventHandler completedHandlerMainPage = delegate { };

                completedHandlerMainPage = delegate
                {
                    storyboard.Completed -= completedHandlerMainPage;
                    storyboard.Stop();
                };

                storyboard.Completed += completedHandlerMainPage;
                storyboard.Begin();
            }
            catch (InvalidOperationException) { }
        }
    }
}
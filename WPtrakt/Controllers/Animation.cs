using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace WPtrakt.Controllers
{
    public class Animation
    {
        public static void NavigateToFadeOut(PhoneApplicationPage page, UIElement targetElement, Uri targetPage)
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
    }
}

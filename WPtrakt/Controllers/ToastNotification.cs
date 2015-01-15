using Coding4Fun.Toolkit.Controls;
using System;

namespace WPtrakt.Controllers
{
    public class ToastNotification
    {
        public static void ShowToast(String title, String message)
        {
            var toast = new ToastPrompt
            {
                Title = title,
                TextOrientation = System.Windows.Controls.Orientation.Vertical,
                Message = message,
            };
            toast.Show();
        }
    }
}

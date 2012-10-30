using System;
using Coding4Fun.Phone.Controls;

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

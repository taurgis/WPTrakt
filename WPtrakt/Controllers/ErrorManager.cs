using System.Windows;

namespace WPtrakt.Controllers
{
    public class ErrorManager
    {
        public static void ShowConnectionErrorPopup()
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ToastNotification.ShowToast("Error!", "Error connecting to server, please try to refresh (Menu).");
            });
        }
    }
}

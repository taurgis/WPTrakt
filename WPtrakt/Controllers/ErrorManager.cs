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
using Microsoft.Phone.Shell;

namespace VPtrakt.Controllers
{
    public class ErrorManager
    {
        public static void ShowConnectionErrorPopup()
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show("Error connecting to server, please try to refresh (Menu).");
            });
        }
    }
}

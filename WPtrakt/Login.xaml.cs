using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPtrakt.Model;
using WPtrakt.Controllers;
using System.Reflection;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using WPtrakt.Model.Trakt.Request;
using System.IO.IsolatedStorage;
using WPtrakt.Model.Trakt;
using System.Windows.Media;

namespace WPtrakt
{
    public partial class Login : PhoneApplicationPage
    {
        public Login()
        {
            InitializeComponent();
            this.Loaded += Login_Loaded;
        }

        void Login_Loaded(object sender, RoutedEventArgs e)
        {
            bool dark = ((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"] == Visibility.Visible);

            if (!dark)
            {
                EmailGrid.Background = new SolidColorBrush(Colors.White);
                LoginGrid.Background = new SolidColorBrush(Colors.White); 
                PasswordGrid.Background = new SolidColorBrush(Colors.White);
                LabelLogin.Foreground = new SolidColorBrush(Colors.Black);
                LabelMail.Foreground = new SolidColorBrush(Colors.Black);
                LabelPassword.Foreground = new SolidColorBrush(Colors.Black); 
            }

            if (!String.IsNullOrEmpty(AppUser.Instance.UserName))
                this.LoginBox.Text = AppUser.Instance.UserName;

            if (!String.IsNullOrEmpty(AppUser.Instance.Password))
                this.PasswordBox.Password = AppUser.Instance.Password;
        }

        private void SigninButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(this.LoginBox.Text) && !String.IsNullOrEmpty(this.PasswordBox.Password))
            {
                AppUser.Instance.UserName = this.LoginBox.Text;
                AppUser.Instance.Password = this.PasswordBox.Password;

                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri("https://api.trakt.tv/account/test/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName));
                request.Method = "POST";
                request.BeginGetRequestStream(new AsyncCallback(GetValidationRequestStreamCallback), request);
                SigninButton.Visibility = System.Windows.Visibility.Collapsed;
                JoinButton.Visibility = System.Windows.Visibility.Collapsed;
                progressBar.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                ToastNotification.ShowToast("Error!", "Please fill in all required fields!");
            }

        }

        void GetValidationRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadValidationStringCompleted), webRequest);
        }

        void client_DownloadValidationStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                HttpStatusCode status = httpResoponse.StatusCode;

                if (status == System.Net.HttpStatusCode.OK)
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                    NavigationService.GoBack();
                    ToastNotification.ShowToast("Success!", "You have been logged in!");
                    });
                }
            }
            catch (WebException)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    SigninButton.Visibility = System.Windows.Visibility.Visible;
                    JoinButton.Visibility = System.Windows.Visibility.Visible;
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    ToastNotification.ShowToast("Error!", "Login data incorrect, or server connection problems.");
                });


            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

        }

        private void JoinButton_Click_1(object sender, RoutedEventArgs e)
        {
            EmailGrid.Visibility = System.Windows.Visibility.Visible;
            EmailStackPanel.Visibility = System.Windows.Visibility.Visible;
            SigninButton.Visibility = System.Windows.Visibility.Collapsed;
            JoinButton.Visibility = System.Windows.Visibility.Collapsed;
            CreateButton.Visibility = System.Windows.Visibility.Visible;
            Cancelbutton.Visibility = System.Windows.Visibility.Visible;
        }

        private void Cancelbutton_Click_1(object sender, RoutedEventArgs e)
        {
            EmailGrid.Visibility = System.Windows.Visibility.Collapsed;
            EmailStackPanel.Visibility = System.Windows.Visibility.Collapsed;
            SigninButton.Visibility = System.Windows.Visibility.Visible;
            JoinButton.Visibility = System.Windows.Visibility.Visible;
            CreateButton.Visibility = System.Windows.Visibility.Collapsed;
            Cancelbutton.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void CreateButton_Click_1(object sender, RoutedEventArgs e)
        {
            string MatchEmailPattern =
          @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
   + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
   + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
   + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";

            if (String.IsNullOrEmpty(LoginBox.Text) || String.IsNullOrEmpty(PasswordBox.Password) || String.IsNullOrEmpty(MailBox.Text))
            {
                ToastNotification.ShowToast("Error!", "Please fill in all fields");
                return;
            }

            if (Regex.IsMatch(MailBox.Text, MatchEmailPattern))
            {
                CreateButton.IsEnabled = false;
                Cancelbutton.IsEnabled = false;
                AppUser.Instance.UserName = LoginBox.Text;
                AppUser.Instance.Password = PasswordBox.Password;
                progressBar.Visibility = System.Windows.Visibility.Visible;
                var registerClient = new WebClient();
                registerClient.UploadStringCompleted += new UploadStringCompletedEventHandler(registerClient_UploadStringCompleted);

                RegisterAuth auth = new RegisterAuth();
                auth.Email = MailBox.Text;
               
                registerClient.UploadStringAsync(new Uri("https://api.trakt.tv/account/create/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(RegisterAuth), auth));
            }
            else
                 ToastNotification.ShowToast("Error!", "Invalid email!");
        }

        void registerClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {

            try
            {
                String jsonString = e.Result;
                if (jsonString.Contains(" is already a registered e-mail"))
                {
                   ToastNotification.ShowToast("Error!", "Email already in use.");
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    CreateButton.IsEnabled = true;
                    Cancelbutton.IsEnabled = true;
                    return;
                }

                if (jsonString.Contains("is already a registered username"))
                {
                   ToastNotification.ShowToast("Error!", "Username already in use.");
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    CreateButton.IsEnabled = true;
                    Cancelbutton.IsEnabled = true;
                    return;
                }

                if (jsonString.Contains("created account for"))
                {
                    try
                    {
                        IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(TraktProfile.getFolderStatic() + "/" + AppUser.Instance.UserName + ".json");
                    }
                    catch (IsolatedStorageException) { }
                    App.ViewModel.Profile = null;
                    NavigationService.GoBack();
                    ToastNotification.ShowToast("Success!", "Successfully created an account!");
                  
                }
            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException) { ErrorManager.ShowConnectionErrorPopup(); }

        }
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            NavigationService.RemoveBackEntry();
            base.OnBackKeyPress(e);
        }
    }
}
using Microsoft.Phone.Controls;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using WPtrakt.Controllers;
using WPtrakt.Model;
using WPtraktBase.Controller;

namespace WPtrakt
{
    public partial class Login : PhoneApplicationPage
    {
        private UserController userController;
        public Login()
        {
            InitializeComponent();
            userController = new UserController();
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

        private async void SigninButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(this.LoginBox.Text) && !String.IsNullOrEmpty(this.PasswordBox.Password))
            {
                AppUser.Instance.UserName = this.LoginBox.Text;
                AppUser.Instance.Password = this.PasswordBox.Password;
 
                SigninButton.Visibility = System.Windows.Visibility.Collapsed;
                JoinButton.Visibility = System.Windows.Visibility.Collapsed;
                progressBar.Visibility = System.Windows.Visibility.Visible;


                if (await userController.ValidateUser())
                {
                    NavigationService.GoBack();
                    ToastNotification.ShowToast("Success!", "You have been logged in!");
                }
                else
                {
                    SigninButton.Visibility = System.Windows.Visibility.Visible;
                    JoinButton.Visibility = System.Windows.Visibility.Visible;
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    ToastNotification.ShowToast("Error!", "Login data incorrect, or server connection problems.");
                }
            }
            else
            {
                ToastNotification.ShowToast("Error!", "Please fill in all required fields!");
            }
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

        private async void CreateButton_Click_1(object sender, RoutedEventArgs e)
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
                CreateButton.Visibility = System.Windows.Visibility.Collapsed;
                Cancelbutton.Visibility = System.Windows.Visibility.Collapsed; ;
                AppUser.Instance.UserName = LoginBox.Text;
                AppUser.Instance.Password = PasswordBox.Password;
                progressBar.Visibility = System.Windows.Visibility.Visible;

                RegistrationResult result = await userController.CreateUser(LoginBox.Text, PasswordBox.Password, MailBox.Text);


                if (result == RegistrationResult.EMAILINUSE)
                {
                    ToastNotification.ShowToast("Error!", "Email already in use.");
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    CreateButton.Visibility = System.Windows.Visibility.Visible;
                    Cancelbutton.Visibility = System.Windows.Visibility.Visible;
                    return;
                }

                else if (result == RegistrationResult.USERNAMEINUSE)
                {
                    ToastNotification.ShowToast("Error!", "Username already in use.");
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    CreateButton.Visibility = System.Windows.Visibility.Visible;
                    Cancelbutton.Visibility = System.Windows.Visibility.Visible;
                    return;
                }

                else if (result == RegistrationResult.OK)
                {
                    App.ViewModel.Profile = null;
                    NavigationService.GoBack();
                    ToastNotification.ShowToast("Success!", "Successfully created an account!");

                }
                else if (result == RegistrationResult.FAILED)
                {
                    ToastNotification.ShowToast("Error!", "Problem connecting to the server!");
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    CreateButton.Visibility = System.Windows.Visibility.Visible;
                    Cancelbutton.Visibility = System.Windows.Visibility.Visible;
                    return;
                }
            }
            else
                 ToastNotification.ShowToast("Error!", "Invalid email!");
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            NavigationService.RemoveBackEntry();
            base.OnBackKeyPress(e);
        }
    }
}
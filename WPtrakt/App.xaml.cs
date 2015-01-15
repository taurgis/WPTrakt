using System.Windows;
using System;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPtrakt.Model;
using System.Threading.Tasks;

namespace WPtrakt
{
    public partial class App : Application
    {
        private static MainViewModel viewModel = null;
        private static SearchViewModel searchViewModel = null;
        private static SettingsViewModel settingsViewModel = null;
        private static MyMoviesViewModel myMoviesViewModel = null;
        private static MyShowsViewModel myShowsViewModel = null;
        private static MovieViewModel movieViewModel = null;
        private static ShowViewModel showViewModel = null;
        private static EpisodeViewModel episodeViewModel = null;
        private static ActivityViewModel activityViewModel = null;
        private static CheckinHistoryViewModel checkinHistoryViewModel = null;
        private static FullTrendingViewModel trendingViewModel = null;
        private static FriendsViewModel friendsViewModel = null;
        private static FriendViewModel friendViewModel = null;
        private static SearchUsersViewModel searchUsersViewModel = null;
        private static UpcommingViewModel upcommingViewModel = null;

        private bool reset;
        public PhoneApplicationFrame RootFrame { get; private set; }
        

        #region Getters/Setters

        public static MainViewModel ViewModel
        {
            get
            {
                if (viewModel == null)
                    viewModel = new MainViewModel();

                return viewModel;
            }
            set
            {
                viewModel = value;
            }

        }

        public static SearchUsersViewModel SearchUsersViewModel
        {
            get
            {
                if (searchUsersViewModel == null)
                    searchUsersViewModel = new SearchUsersViewModel();

                return searchUsersViewModel;
            }
            set
            {
                searchUsersViewModel = value;
            }

        }

        private static Main mainPage;
        public static Main MainPage
        {
            get
            {

                return mainPage;
            }
            set
            {
                mainPage = value;
            }

        }

        public static FullTrendingViewModel TrendingViewModel
        {
            get
            {
                if (trendingViewModel == null)
                    trendingViewModel = new FullTrendingViewModel();

                return trendingViewModel;
            }
        }

        public static SearchViewModel SearchViewModel
        {
            get
            {
                if (searchViewModel == null)
                    searchViewModel = new SearchViewModel();

                return searchViewModel;
            }
        }

        public static UpcommingViewModel UpcommingViewModel
        {
            get
            {
                if (upcommingViewModel == null)
                    upcommingViewModel = new UpcommingViewModel();

                return upcommingViewModel;
            }
        }

        public static ActivityViewModel ActivityViewModel
        {
            get
            {
                if (activityViewModel == null)
                    activityViewModel = new ActivityViewModel();

                return activityViewModel;
            }
        }

        public static MyShowsViewModel MyShowsViewModel
        {
            get
            {
                if (myShowsViewModel == null)
                    myShowsViewModel = new MyShowsViewModel();

                return myShowsViewModel;
            }
            set
            {
                myShowsViewModel = value;
            }
        }
        
        public static MyMoviesViewModel MyMoviesViewModel
        {
            get
            {
                if (myMoviesViewModel == null)
                    myMoviesViewModel = new MyMoviesViewModel();

                return myMoviesViewModel;
            }
            set
            {
                myMoviesViewModel = value;
            }
        }

        public static MovieViewModel MovieViewModel
        {
            get
            {
                if (movieViewModel == null)
                    movieViewModel = new MovieViewModel();

                return movieViewModel;
            }
            set
            {
                movieViewModel = value;
            }
        }

        public static ShowViewModel ShowViewModel
        {
            get
            {
                if (showViewModel == null)
                    showViewModel = new ShowViewModel();

                return showViewModel;
            }
            set
            {
                showViewModel = value;
            }
        }


        public static EpisodeViewModel EpisodeViewModel
        {
            get
            {
                if (episodeViewModel == null)
                    episodeViewModel = new EpisodeViewModel();

                return episodeViewModel;
            }
            set
            {
                episodeViewModel = value;
            }
        }

        public static SettingsViewModel SettingsViewModel
        {
            get
            {
                if (settingsViewModel == null)
                    settingsViewModel = new SettingsViewModel();

                return settingsViewModel;
            }

            set
            {
                settingsViewModel = value;
            }
        }

        public static CheckinHistoryViewModel CheckinHistoryViewModel
        {
            get
            {
                if (checkinHistoryViewModel == null)
                    checkinHistoryViewModel = new CheckinHistoryViewModel();

                return checkinHistoryViewModel;
            }

            set
            {
                checkinHistoryViewModel = value;
            }
        }

        public static FriendsViewModel FriendsViewModel
        {
            get
            {
                if (friendsViewModel == null)
                    friendsViewModel = new FriendsViewModel();

                return friendsViewModel;
            }

            set
            {
                friendsViewModel = value;
            }
        }

        public static FriendViewModel FriendViewModel
        {
            get
            {
                if (friendViewModel == null)
                    friendViewModel = new FriendViewModel();

                return friendViewModel;
            }

            set
            {
                friendViewModel = value;
            }
        }


        #endregion

        public static ProgressIndicator ShowLoading(PhoneApplicationPage page)
        {
            var indicator = new ProgressIndicator
            {
                IsVisible = true,
                IsIndeterminate = true,
                Text = "Loading..."
            };

            if (page != null)
                SystemTray.SetProgressIndicator(page, indicator);

            return indicator;
        }

        public App()
        {
            UnhandledException += Application_UnhandledException;

            InitializeComponent();

            InitializePhoneApplication();

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Application.Current.Host.Settings.EnableFrameRateCounter = true;
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public static async void TrackEvent(String category, String action)
        {
            await TrackEventAsync(category, action);
        }

        private static Task TrackEventAsync(String category, String action)
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendEvent(category, action, Environment.OSVersion.Version.ToString() , 0);
                tcs.TrySetResult(true);
            }
            catch (Exception)
            {
                tcs.TrySetResult(false);
            }
            return tcs.Task;
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new Delay.HybridOrientationChangesFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;
            RootFrame.Navigating += RootFrame_Navigating;
            RootFrame.Navigated += RootFrame_Navigated;
            RootFrame.UriMapper = new AssociationUriMapper();

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (reset && e.IsCancelable && e.Uri.OriginalString == "/MainPage.xaml")
            {
                e.Cancel = true;
                reset = false;
            }
        }

        void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            reset = e.NavigationMode == NavigationMode.Reset;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

       

        #endregion

   
    }

    class AssociationUriMapper : UriMapperBase
    {
        public override Uri MapUri(Uri uri)
        {
            TrackPages(uri);

            // send all links to MainPage.xaml by default
            Uri newUri = uri;

            string uriAsString = uri.ToString();

            // Before navigating to one of the contact binding pages, check to make sure the user is signed in
            if (!String.IsNullOrEmpty(AppUser.Instance.UserName) )
            {
                // change the deep link URI to land on the right page
                // e.g.: /PeopleExtension?action=Show_Contact&contact_ids=2002
                // becomes /MainPage.xaml?action=Show_Contact&contact_ids=2002

                if (uriAsString.Contains("Show_Contact"))
                {
                    // When the system includes "Show_Contact" in the uri, the user tapped the connect tile for
                    // one of your app's contact bindings.

                    // keep the parameters and send to MainPage.xaml
                    string offsetQueryParams = uri.ToString().Substring(uriAsString.IndexOf('&') + 1);
                    string[] args = offsetQueryParams.Split('=');

                    if (args[1].Contains(","))
                        args[1] = args[1].Split(',')[0];

                    newUri = new Uri("/Friend.xaml?isTile=true&friendid=" + args[1], UriKind.Relative);
                }
                else if (uriAsString.Contains("Connect_Contact"))
                {
                    // This is an optional scenario. See ManualConnect.xaml.cs.
                    // When the system includes "Connect_Contact" in the uri, the user is attempting to 
                    // manually bind a contact to your app.

                    string offsetQueryParams = uri.ToString().Substring(uriAsString.IndexOf('&') + 1);
                    string[] args = offsetQueryParams.Split('=');

                    if (args[0] == "binding_id")
                    {
                        // Map the uri to ShowContact.xaml and include the ID of the contact binding
                        // in the query string params
                        return new Uri("/Friends.xaml?id=" + args[1], UriKind.Relative);
                    }
                }

            }

            System.Diagnostics.Debug.WriteLine("AssociationUriMapper (new) : " + newUri.ToString());
            return newUri;
        }

        private async void TrackPages(Uri uri)
        {
            await TrackPagesAsync(uri);
        }

        private Task TrackPagesAsync(Uri uri)
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                GoogleAnalytics.EasyTracker.GetTracker().SendView(uri.ToString());
                tcs.TrySetResult(true);
            }catch(Exception)
            {
                tcs.TrySetResult(false);
            }
            return tcs.Task;
        }

      
    }

}
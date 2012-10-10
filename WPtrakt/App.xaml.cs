using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

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

        public static SearchViewModel SearchViewModel
        {
            get
            {
                if (searchViewModel == null)
                    searchViewModel = new SearchViewModel();

                return searchViewModel;
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
        }

        #endregion

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

        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
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
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
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
}
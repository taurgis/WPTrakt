using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPtrakt.ViewModels;
using WPtrakt.Model.Trakt;
using WPtrakt.Controllers;
using WPtraktBase.Controller;
using WPtrakt.Model;
using System.IO;
using System.Collections.ObjectModel;
using System.Reflection;
using WPtraktBase.Controllers;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WPtrakt
{
    public partial class Upcomming : PhoneApplicationPage
    {
        private UserController userController;
        private ProgressIndicator indicator;

        public Upcomming()
        {
            InitializeComponent();

            userController = new UserController();
            this.DataContext = App.UpcommingViewModel;
            this.Loaded += Upcomming_Loaded;
        }

        private void Upcomming_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Opacity = 1;
            this.indicator = App.ShowLoading(this);
            LoadCalendarData();
            
        }

        public void LoadCalendarData()
        {
     
                App.UpcommingViewModel.CalendarItems = new ObservableCollection<CalendarListItemViewModel>();
                App.UpcommingViewModel.NotifyPropertyChanged("CalendarItems");
                App.UpcommingViewModel.NotifyPropertyChanged("LoadingStatus");
                HttpWebRequest request;

                request = (HttpWebRequest)WebRequest.Create(new Uri("https://api.trakt.tv/user/calendar/shows.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName + "/" + DateTime.Now.ToString("yyyyMMdd") + "/14"));
                request.Method = "POST";
                request.BeginGetRequestStream(new AsyncCallback(GetCalendarRequestStreamCallback), request);
            
        }

        void GetCalendarRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)asynchronousResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(asynchronousResult);

            byte[] byteArray = Encoding.UTF8.GetBytes(AppUser.createJsonStringForAuthentication());

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(client_DownloadCalendarStringCompleted), webRequest);
        }

        void client_DownloadCalendarStringCompleted(IAsyncResult r)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)r.AsyncState;
                HttpWebResponse httpResoponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                System.Net.HttpStatusCode status = httpResoponse.StatusCode;
                if (status == System.Net.HttpStatusCode.OK)
                {
                    String jsonString = new StreamReader(httpResoponse.GetResponseStream()).ReadToEnd();
                    ObservableCollection<CalendarListItemViewModel> tempItems = new ObservableCollection<CalendarListItemViewModel>();
                    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                    {
                        //parse into jsonser
                        var ser = new DataContractJsonSerializer(typeof(TraktCalendar[]));
                        TraktCalendar[] obj = (TraktCalendar[])ser.ReadObject(ms);
                        StorageController.saveObjectInMainFolder(obj, typeof(TraktCalendar[]), "upcomming.json");

                        foreach (TraktCalendar calendarDate in obj)
                        {
                            if ((DateTime.Parse(calendarDate.Date) - DateTime.Now).Days > 20)
                                break;
                            ObservableCollection<ListItemViewModel> tempEpisodes = new ObservableCollection<ListItemViewModel>();
                            foreach (TraktCalendarEpisode episode in calendarDate.Episodes)
                            {
                                tempEpisodes.Add(new ListItemViewModel() { Name = episode.Show.Title, ImageSource = episode.Episode.Images.Screen, Imdb = episode.Show.imdb_id + episode.Episode.Season + episode.Episode.Number, SubItemText = episode.Episode.Title, TruncateTitle = false, Tvdb = episode.Show.tvdb_id, Episode = episode.Episode.Number, Season = episode.Episode.Season });

                            }
                            tempItems.Add(new CalendarListItemViewModel() { DateString = calendarDate.Date, Items = tempEpisodes });
                        }

                        if (obj.Length == 00)
                        {
                            tempItems.Add(new CalendarListItemViewModel() { DateString = "No upcomming episodes", Items = new ObservableCollection<ListItemViewModel>() });
                        }

                        App.UpcommingViewModel.CalendarItems = tempItems;
                       

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            this.indicator.IsVisible = false;
                            App.UpcommingViewModel.NotifyPropertyChanged("LoadingStatus");
                            App.UpcommingViewModel.NotifyPropertyChanged("CalendarItems");
                        });
                    }

                }

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            catch (TargetInvocationException)
            { ErrorManager.ShowConnectionErrorPopup(); }
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListItemViewModel model = (ListItemViewModel)((StackPanel)sender).DataContext;
            Animation.NavigateToFadeOut(this, LayoutRoot, new Uri("/ViewEpisode.xaml?id=" + model.Tvdb + "&season=" + model.Season + "&episode=" + model.Episode, UriKind.Relative));
        }

        private void PhoneApplicationPage_BackKeyPress_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Animation.FadeOut(LayoutRoot);
        }



    }


}
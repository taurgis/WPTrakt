using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Primitives;
using WPtrakt.Controllers;
using WPtrakt.Model.Trakt.Request;
using WPtrakt.Model;

namespace WPtrakt
{
    public partial class RatingSelector : PhoneApplicationPage
    {
        public RatingSelector()
        {
            InitializeComponent();
            this.selector.DataSource = new IntLoopingDataSource() { MinValue = 1, MaxValue = 10, SelectedItem = 1 };
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            String type;
            NavigationContext.QueryString.TryGetValue("type", out type);
            if (type.Equals("movie"))
                RateMovie();
            else if (type.Equals("show"))
                RateShow();
            else
                RateEpisode();
        }

        private void RateEpisode()
        {
            String imdb;
            String year;
            String title;
            String season;
            String episode;

            NavigationContext.QueryString.TryGetValue("imdb", out imdb);
            NavigationContext.QueryString.TryGetValue("year", out year);
            NavigationContext.QueryString.TryGetValue("title", out title);
            NavigationContext.QueryString.TryGetValue("season", out season);
            NavigationContext.QueryString.TryGetValue("episode", out episode);

            var ratingClient = new WebClient();

            ratingClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadRatingStringCompleted);
            RatingAuth auth = new RatingAuth();

            auth.Imdb = imdb;
            auth.Title = title;
            auth.Year = Int16.Parse(year);
            auth.Season = Int16.Parse(season);
            auth.Episode = Int16.Parse(episode);
            auth.Rating = Int16.Parse(this.selector.DataSource.SelectedItem.ToString());
            ratingClient.UploadStringAsync(new Uri("http://api.trakt.tv/rate/episode/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(RatingAuth), auth));
      
        }

        private void RateShow()
        {
            String imdb;
            String year;
            String title;

            NavigationContext.QueryString.TryGetValue("imdb", out imdb);
            NavigationContext.QueryString.TryGetValue("year", out year);
            NavigationContext.QueryString.TryGetValue("title", out title);

            var ratingClient = new WebClient();

            ratingClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadRatingStringCompleted);
            RatingAuth auth = new RatingAuth();

            auth.Imdb = imdb;
            auth.Title = title;
            auth.Year = Int16.Parse(year);
            auth.Rating = Int16.Parse(this.selector.DataSource.SelectedItem.ToString());
            ratingClient.UploadStringAsync(new Uri("http://api.trakt.tv/rate/show/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(RatingAuth), auth));
        }

        private void RateMovie()
        {
            String imdb;
            String year;
            String title;

            NavigationContext.QueryString.TryGetValue("imdb", out imdb);
            NavigationContext.QueryString.TryGetValue("year", out year);
            NavigationContext.QueryString.TryGetValue("title", out title);

            var ratingClient = new WebClient();

            ratingClient.UploadStringCompleted += new UploadStringCompletedEventHandler(client_UploadRatingStringCompleted);
            RatingAuth auth = new RatingAuth();

            auth.Imdb = imdb;
            auth.Title = title;
            auth.Year = Int16.Parse(year);
            auth.Rating = Int16.Parse(this.selector.DataSource.SelectedItem.ToString());
            ratingClient.UploadStringAsync(new Uri("http://api.trakt.tv/rate/movie/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(RatingAuth), auth));
        }

        void client_UploadRatingStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            try
            {
                String jsonString = e.Result;
                MessageBox.Show("Rated successfull.");

            }
            catch (WebException)
            {
                ErrorManager.ShowConnectionErrorPopup();
            }
            NavigationService.GoBack();
        }
    }

    public abstract class LoopingDataSourceBase : ILoopingSelectorDataSource
    {
        private object selectedItem;

        #region ILoopingSelectorDataSource Members

        public abstract object GetNext(object relativeTo);

        public abstract object GetPrevious(object relativeTo);

        public object SelectedItem
        {
            get
            {
                return this.selectedItem;
            }
            set
            {
                // this will use the Equals method if it is overridden for the data source item class
                if (!object.Equals(this.selectedItem, value))
                {
                    // save the previously selected item so that we can use it 
                    // to construct the event arguments for the SelectionChanged event
                    object previousSelectedItem = this.selectedItem;
                    this.selectedItem = value;
                    // fire the SelectionChanged event
                    this.OnSelectionChanged(previousSelectedItem, this.selectedItem);
                }
            }
        }

        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        protected virtual void OnSelectionChanged(object oldSelectedItem, object newSelectedItem)
        {
            EventHandler<SelectionChangedEventArgs> handler = this.SelectionChanged;
            if (handler != null)
            {
                handler(this, new SelectionChangedEventArgs(new object[] { oldSelectedItem }, new object[] { newSelectedItem }));
            }
        }

        #endregion
    }

    public class IntLoopingDataSource : LoopingDataSourceBase
    {
        private int minValue;
        private int maxValue;
        private int increment;

        public IntLoopingDataSource()
        {
            this.MaxValue = 10;
            this.MinValue = 0;
            this.Increment = 1;
            this.SelectedItem = 0;
        }

        public int MinValue
        {
            get
            {
                return this.minValue;
            }
            set
            {
                if (value >= this.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("MinValue", "MinValue cannot be equal or greater than MaxValue");
                }
                this.minValue = value;
            }
        }

        public int MaxValue
        {
            get
            {
                return this.maxValue;
            }
            set
            {
                if (value <= this.MinValue)
                {
                    throw new ArgumentOutOfRangeException("MaxValue", "MaxValue cannot be equal or lower than MinValue");
                }
                this.maxValue = value;
            }
        }

        public int Increment
        {
            get
            {
                return this.increment;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("Increment", "Increment cannot be less than or equal to zero");
                }
                this.increment = value;
            }
        }

        public override object GetNext(object relativeTo)
        {
            int nextValue = (int)relativeTo + this.Increment;
            if (nextValue > this.MaxValue)
            {
                nextValue = this.MinValue;
            }
            return nextValue;
        }

        public override object GetPrevious(object relativeTo)
        {
            int prevValue = (int)relativeTo - this.Increment;
            if (prevValue < this.MinValue)
            {
                prevValue = this.MaxValue;
            }
            return prevValue;
        }
    }
}
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPtrakt.Model;
using WPtrakt.Model.Trakt;
using WPtrakt.ViewModels;

namespace WPtrakt.Views
{
    public class ViewWithHistory : PhoneApplicationPage
    {
        protected ActivityListItemViewModel DetermineActivityWithFilter(int type, TraktActivity activity)
        {
            ActivityListItemViewModel tempModel = null;

            try
            {

                switch (activity.Action)
                {
                    case "watchlist":
                        if (type == 4 || (type >= 0 && type < 4))
                        {
                            if (type == 1 && activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }
                            else if (type == 2 && !activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }

                            tempModel = AddToWatchList(activity);
                        }
                        break;
                    case "rating":
                        if (type == 5 || (type >= 0 && type < 4))
                        {
                            if (type == 1 && activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }
                            else if (type == 2 && !activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }

                            tempModel = Rated(activity);
                        }
                        break;
                    case "checkin":
                        if (type == 6 || (type >= 0 && type < 4))
                        {
                            if (type == 1 && activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }
                            else if (type == 2 && !activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }

                            tempModel = Checkin(activity);
                        }
                        break;
                    case "scrobble":
                        if (type == 7 || (type >= 0 && type < 4))
                        {
                            if (type == 1 && activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }
                            else if (type == 2 && !activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }

                            tempModel = Scrobble(activity);
                        }
                        break;
                    case "shout":
                        if (type == 8 || (type >= 0 && type < 4))
                        {
                            if (type == 1 && activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }
                            else if (type == 2 && !activity.User.Username.Equals(AppUser.Instance.UserName))
                            {
                                break;
                            }

                            tempModel = Shout(activity);
                        }
                        break;
                }



            }
            catch (NullReferenceException) { }
            return tempModel;
        }

        protected ActivityListItemViewModel DetermineActivity(TraktActivity activity, ActivityListItemViewModel tempModel)
        {
            switch (activity.Action)
            {
                case "watchlist":
                    tempModel = AddToWatchList(activity);
                    break;
                case "rating":
                    tempModel = Rated(activity);
                    break;
                case "checkin":
                    tempModel = Checkin(activity);
                    break;
                case "scrobble":
                    tempModel = Scrobble(activity);
                    break;
                case "shout":
                    tempModel = Shout(activity);
                    break;
            }

            return tempModel;
        }

        protected ActivityListItemViewModel Shout(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to movie " + activity.Movie.Title + "", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to show " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added shout to episode " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        protected ActivityListItemViewModel Seen(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "saw " + activity.Movie.Title + ".", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "saw " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "saw " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        protected ActivityListItemViewModel Scrobble(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "scrobbled " + activity.Movie.Title + "", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "scrobbled " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "scrobbled " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        protected ActivityListItemViewModel Checkin(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "checked in " + activity.Movie.Title, Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "checked in " + activity.Show.Title + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "checked in " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ".", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        protected ActivityListItemViewModel Rated(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "rated " + activity.Movie.Title + ": " + activity.RatingAdvanced + "/10 ", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "rated " + activity.Show.Title + ": " + activity.RatingAdvanced + "/10 .", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "rated " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + ": " + activity.RatingAdvanced + "/10 .", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }
            return null;
        }

        protected ActivityListItemViewModel AddToWatchList(TraktActivity activity)
        {
            switch (activity.Type)
            {
                case "movie":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Movie.Title + " to the watchlist", Name = activity.Movie.Title, TimeStamp = activity.TimeStamp, Imdb = activity.Movie.imdb_id, Screen = activity.Movie.Images.Poster, Type = "movie", Year = activity.Movie.year };
                case "show":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Show.Title + " to the watchlist.", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Imdb = activity.Show.imdb_id, Screen = activity.Show.Images.Poster, Type = "show", Year = activity.Show.year };
                case "episode":
                    return new ActivityListItemViewModel() { Activity = ((activity.User.Username.Equals(AppUser.Instance.UserName)) ? "You " : activity.User.Username + " ") + "added " + activity.Show.Title + " - " + activity.Episode.Title + " ( " + activity.Episode.Season + "x" + activity.Episode.Number + " ) " + "to the watchlist.", Name = activity.Show.Title, TimeStamp = activity.TimeStamp, Tvdb = activity.Show.tvdb_id, Screen = activity.Episode.Images.Screen, Type = "episode", Season = Int16.Parse(activity.Episode.Season), Episode = Int16.Parse(activity.Episode.Number), Year = activity.Show.year };
            }

            return null;
        }

        protected void SortHistoryByDate(TraktActivity activity, ActivityListItemViewModel tempModel, ref Dictionary<DateTime, List<ActivityListItemViewModel>> sortedOrderHistory)
        {
            if (sortedOrderHistory == null)
            {
                sortedOrderHistory = new Dictionary<DateTime, List<ActivityListItemViewModel>>();
            }


            DateTime time = new DateTime(1970, 1, 1, 0, 0, 9, DateTimeKind.Utc);
            time = time.AddSeconds(activity.TimeStamp);
            time = time.ToLocalTime();
            DateTime onlyDay = new DateTime(time.Year, time.Month, time.Day);

            tempModel.Date = onlyDay;

            if (sortedOrderHistory.ContainsKey(onlyDay))
            {

                sortedOrderHistory[onlyDay].Add(tempModel);
            }
            else
            {
                List<ActivityListItemViewModel> tempList = new List<ActivityListItemViewModel>();
                tempList.Add(tempModel);
                sortedOrderHistory.Add(onlyDay, tempList);
            }
        }

    }
}

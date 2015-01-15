using System;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPtrakt.Model;
using WPtraktBase.Model.Trakt;
using WPtrakt.Model.Trakt;
using WPtraktBase.Controllers;
using WPtrakt.Model.Trakt.Request;
using System.Collections.Generic;

namespace WPtraktBase.DAO
{
    public class UserDao
    {
        internal async Task<Boolean> ValidateUser()
        {
            try
            {
                var userClient = new WebClient();
                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/account/test/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + Uri.EscapeDataString(AppUser.Instance.UserName)), AppUser.createJsonStringForAuthentication());

                return jsonString.Contains("all good!");
            }
            catch (WebException)
            { Debug.WriteLine("WebException in FetchLastActivity()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in FetchLastActivity()."); }

            return false;
        }

        internal async Task<RegistrationResult> CreateUser(String username, String password, String email)
        {
            try
            {
                var userClient = new WebClient();

                RegisterAuth auth = new RegisterAuth();
                auth.Email = email;
                auth.Username = username;
                auth.Password = AppUser.ShaPassword(password);

                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/account/create/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(RegisterAuth), auth));

                if (jsonString.Contains(" is already a registered e-mail"))
                {
                    return RegistrationResult.EMAILINUSE;
                }

                if (jsonString.Contains("is already a registered username"))
                {
                    return RegistrationResult.USERNAMEINUSE;
                }

                if (jsonString.Contains("created account for"))
                {
                    return RegistrationResult.OK;
                }

                return RegistrationResult.OK;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in CreateUser()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in CreateUser()."); }

            return RegistrationResult.FAILED;
        }

        internal async Task<TraktProfile> GetUserProfile()
        {
            try
            {
                var userClient = new WebClient();
                TraktProfile profile = null;
                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/user/profile.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    if (jsonString.Contains("watching\":[]"))
                    {
                        var ser = new DataContractJsonSerializer(typeof(TraktProfile));

                        profile = (TraktProfile)ser.ReadObject(ms);
                    }
                    else
                    {
                        var ser = new DataContractJsonSerializer(typeof(TraktProfileWithWatching));

                        profile = (TraktProfileWithWatching)ser.ReadObject(ms);
                    }
                }

                return profile;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in GetUserProfile()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in GetUserProfile()."); }


            return null;
        }

        internal async Task<TraktProfile> GetUserProfile(String username)
        {
            try
            {
                var userClient = new WebClient();
                TraktProfile profile = null;
                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/user/profile.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + username), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    if (jsonString.Contains("watching\":[]"))
                    {
                        var ser = new DataContractJsonSerializer(typeof(TraktProfile));

                        profile = (TraktProfile)ser.ReadObject(ms);
                    }
                    else
                    {
                        var ser = new DataContractJsonSerializer(typeof(TraktProfileWithWatching));

                        profile = (TraktProfileWithWatching)ser.ReadObject(ms);
                    }
                }

                return profile;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in GetUserProfile()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in GetUserProfile()."); }


            return null;
        }

        internal async Task<TraktLastActivity> FetchLastActivity()
        {
            try
            {
                var userClient = new WebClient();
                String jsonString = await userClient.UploadStringTaskAsync(new Uri("http://api.trakt.tv/user/lastactivity.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());


                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktLastActivity));
                    TraktLastActivity activity = (TraktLastActivity)ser.ReadObject(ms);

                    Debug.WriteLine("Fetched last activity from Trakt.");

                    return activity;
                }
            }
            catch (WebException)
            { Debug.WriteLine("WebException in FetchLastActivity()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in FetchLastActivity()."); }

            return null;
        }

        internal async Task<Boolean> CancelActiveCheckin()
        {
            try
            {
                var cancelCheckinClient = new WebClient();

                String jsonString = await cancelCheckinClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/movie/cancelcheckin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication());
                jsonString = await cancelCheckinClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/show/cancelcheckin/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication());
                return true;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in CancelActiveCheckin()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in CancelActiveCheckin()."); }

            return false;
        }

        internal async Task<List<TraktActivity>> getNewsFeed()
        {
            try
            {
                var myFeedClient = new WebClient();
                String myFeedJsonString = await myFeedClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/activity/user.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());

                var friendsFeedClient = new WebClient();
                String friendsFeedJsonString = await myFeedClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/activity/friends.json/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication());

                List<TraktActivity> activity = new List<TraktActivity>();

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(myFeedJsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktFriendsActivity));

                    TraktFriendsActivity myActivity = (TraktFriendsActivity)ser.ReadObject(ms);
                    activity.AddRange(myActivity.Activity);
                    ms.Close();
                }


                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(friendsFeedJsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktFriendsActivity));

                    TraktFriendsActivity friendsActivity = (TraktFriendsActivity)ser.ReadObject(ms);
                    activity.AddRange(friendsActivity.Activity);
                    ms.Close();
                }

                return activity;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getNewsFeed()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getNewsFeed()."); }
            return new List<TraktActivity>();
        }

        internal async Task<List<TraktActivity>> getNewsFeed(String id)
        {
            try
            {
                var myFeedClient = new WebClient();
                String myFeedJsonString = await myFeedClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/activity/user.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + id), AppUser.createJsonStringForAuthentication());

             
                List<TraktActivity> activity = new List<TraktActivity>();

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(myFeedJsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktFriendsActivity));

                    TraktFriendsActivity myActivity = (TraktFriendsActivity)ser.ReadObject(ms);

                    if (myActivity != null)
                    {
                        activity.AddRange(myActivity.Activity);
                    }
                    ms.Close();
                }

                return activity;
            }
            catch(Exception)
            {

            }
           
            return new List<TraktActivity>();
        }

        internal async Task<List<TraktActivity>> getCheckinHistory()
        {
            try
            {
                var myFeedClientScrobble = new WebClient();
                String myFeedJsonString = await myFeedClientScrobble.UploadStringTaskAsync(new Uri("https://api.trakt.tv/activity/user.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName + "/all/scrobble"), AppUser.createJsonStringForAuthentication());
                var myFeedClientCheckin = new WebClient();
                String myFeedJsonStringCheckin = await myFeedClientScrobble.UploadStringTaskAsync(new Uri("https://api.trakt.tv/activity/user.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName + "/all/checkin"), AppUser.createJsonStringForAuthentication());
               
                var myFeedClientSeen = new WebClient();

                String myFeedJsonStringSeen = await myFeedClientSeen.UploadStringTaskAsync(new Uri("https://api.trakt.tv/activity/user.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName + "/all/seen"), AppUser.createJsonStringForAuthentication());
               
           
                List<TraktActivity> activity = new List<TraktActivity>();

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(myFeedJsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktFriendsActivity));

                    TraktFriendsActivity myActivity = (TraktFriendsActivity)ser.ReadObject(ms);
                    activity.AddRange(myActivity.Activity);
                    ms.Close();
                }

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(myFeedJsonStringCheckin)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktFriendsActivity));

                    TraktFriendsActivity myActivity = (TraktFriendsActivity)ser.ReadObject(ms);
                    activity.AddRange(myActivity.Activity);
                    ms.Close();
                }


                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(myFeedJsonStringSeen)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktFriendsActivity));

                    TraktFriendsActivity myActivity = (TraktFriendsActivity)ser.ReadObject(ms);
                    activity.AddRange(myActivity.Activity);
                    ms.Close();
                }


                return activity;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getNewsFeed()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getNewsFeed()."); }
            return new List<TraktActivity>();
        }

        internal async Task<TraktProfile[]> getUserFriends()
        {
            try
            {
                var userClient = new WebClient();
                TraktProfile[] profiles = null;
                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/user/network/friends.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {                    var ser = new DataContractJsonSerializer(typeof(TraktProfile[]));

                  profiles = (TraktProfile[])ser.ReadObject(ms);
                }

                return profiles;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in GetUserFriends()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in GetUserFriends()."); }


            return null;
        }

        internal async Task<TraktProfile[]> searchUsers(String username)
        {
            try
            {
                var userClient = new WebClient();
                TraktProfile[] profiles = null;
           
                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/search/users.json/9294cac7c27a4b97d3819690800aa2fedf0959fa?query=" + username + "&limit=100"), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktProfile[]));

                    profiles = (TraktProfile[])ser.ReadObject(ms);
                }

                return profiles;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in searchUsers()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in searchUsers()."); }


            return null;
        }

        internal async Task<TraktProfile[]> getFollowing()
        {
            try
            {
                var userClient = new WebClient();
                TraktProfile[] profiles = null;
                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/user/network/following.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktProfile[]));

                    profiles = (TraktProfile[])ser.ReadObject(ms);
                }

                return profiles;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getFollowing()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getFollowing()."); }


            return null;
        }

        internal async Task<TraktProfile[]> getFollowers()
        {
            try
            {
                var userClient = new WebClient();
                TraktProfile[] profiles = null;
                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/user/network/followers.json/9294cac7c27a4b97d3819690800aa2fedf0959fa/" + AppUser.Instance.UserName), AppUser.createJsonStringForAuthentication());
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
                {
                    var ser = new DataContractJsonSerializer(typeof(TraktProfile[]));

                    profiles = (TraktProfile[])ser.ReadObject(ms);
                }

                return profiles;
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getFollowers()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getFollowers()."); }


            return null;
        }

        internal async Task<Boolean> FollowUser(String username)
        {
            try
            {
                var userClient = new WebClient();
                FollowAuth auth = new FollowAuth();
               
                auth.User = username;

                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/network/follow/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(FollowAuth),auth));

                return jsonString.Contains("success");
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getFollowers()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getFollowers()."); }


            return false;
        }

        internal async Task<Boolean> UnFollowUser(String username)
        {
            try
            {
                var userClient = new WebClient();
                FollowAuth auth = new FollowAuth();

                auth.User = username;

                String jsonString = await userClient.UploadStringTaskAsync(new Uri("https://api.trakt.tv/network/unfollow/9294cac7c27a4b97d3819690800aa2fedf0959fa"), AppUser.createJsonStringForAuthentication(typeof(FollowAuth), auth));

                return jsonString.Contains("success");
            }
            catch (WebException)
            { Debug.WriteLine("WebException in getFollowers()."); }
            catch (TargetInvocationException)
            { Debug.WriteLine("TargetInvocationException in getFollowers()."); }


            return false;
        }
    }
}

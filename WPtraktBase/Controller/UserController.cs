using Microsoft.Phone.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Phone.PersonalInformation;
using Windows.Storage.Streams;
using WPtrakt.Model.Trakt;
using WPtraktBase.DAO;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.Controller
{
    public class UserController
    {
        private UserDao userDao;

        public UserController()
        {
            userDao = new UserDao();
        }

        public async Task<TraktLastActivity> getLastActivityForUser()
        {
            return await userDao.FetchLastActivity();
        }

        public async Task<Boolean> ValidateUser()
        {
            return await userDao.ValidateUser();
        }

        public async Task<RegistrationResult> CreateUser(String username, String password, String email)
        {
            return await userDao.CreateUser(username, password, email);
        }

        public async Task<TraktProfile> GetUserProfile()
        {
            return await userDao.GetUserProfile();
        }

        public async Task<TraktProfile> GetUserProfile(String username)
        {
            return await userDao.GetUserProfile(username);
        }

        public async Task<Boolean> CancelActiveCheckin()
        {
            return await userDao.CancelActiveCheckin();
        }

        public async Task<List<TraktActivity>> getNewsFeed()
        {
            return await userDao.getNewsFeed();
        }

        public async Task<List<TraktActivity>> getNewsFeed(String id)
        {
            return await userDao.getNewsFeed(id);
        }

        public async Task<List<TraktActivity>> getCheckinHistory()
        {
            return await userDao.getCheckinHistory();
        }


        public async static void ClearContactData()
        {
            ContactBindingManager bindingManager = await ContactBindings.GetAppContactBindingManagerAsync();
            ContactStore store = await ContactStore.CreateOrOpenAsync();

            await store.DeleteAsync();

            await bindingManager.DeleteAllContactBindingsAsync();
        }

        public async Task<List<TraktProfile>> getFriends()
        {
            return new List<TraktProfile>(await userDao.getUserFriends());
        }

        public async Task<List<TraktProfile>> getFollowing()
        {
            return new List<TraktProfile>(await userDao.getFollowing());
        }


        public async Task<List<TraktProfile>> getFollowers()
        {
            return new List<TraktProfile>(await userDao.getFollowers());
        }


        public async Task<List<TraktProfile>> SearchUsers(String query)
        {
            return new List<TraktProfile>(await userDao.searchUsers(query));
        }

        public async Task<Boolean> followUser(String user)
        {
            return await userDao.FollowUser(user);
        }

        public async Task<Boolean> unFollowUser(String user)
        {
            return await userDao.UnFollowUser(user);
        }

        public async void CreateContactBindingsAsync()
        {
            ContactBindingManager bindingManager = await ContactBindings.GetAppContactBindingManagerAsync();

            // Simulate call to web service
            TraktProfile[] profiles = await userDao.getUserFriends();
            ContactStore store = await ContactStore.CreateOrOpenAsync();

            foreach (TraktProfile profile in profiles)
            {
               
                ContactBinding myBinding = bindingManager.CreateContactBinding(profile.Username);

                if (!String.IsNullOrEmpty(profile.Name))
                {
                    if (profile.Name.Contains(" "))
                    {
                        Regex regex = new Regex(@"\s");
                        String[] nameSplit = regex.Split(profile.Name);
                        
                        myBinding.FirstName = nameSplit[0];
                        myBinding.LastName =  nameSplit[1];
                    }
                    else
                    {
                        myBinding.LastName = profile.Name;
                    }                                        
                }

                try
                {
                    if (!String.IsNullOrEmpty(profile.Name) && profile.Name.Contains(" "))
                    {
                        Regex regex = new Regex(@"\s");
                        String[] nameSplit = regex.Split(profile.Name);

                        AddContact(profile.Username, nameSplit[0], nameSplit[1], profile.Username, profile.Avatar, profile.Url);
                    }
                    else
                    {
                        AddContact(profile.Username, "", "", profile.Username, profile.Avatar, profile.Url);
                    }
                    await bindingManager.SaveContactBindingAsync(myBinding);
                }
                catch (Exception e)
                {
                    Console.Write(e.InnerException);
                }

            }
        }

        public async void AddContact(string remoteId, string givenName, string familyName, string username, String avatar, String url)
        {
            ContactStore store = await ContactStore.CreateOrOpenAsync();
            try
            {
                if (await store.FindContactByRemoteIdAsync(remoteId) == null)
                {

                    StoredContact contact = new StoredContact(store);

                    contact.RemoteId = remoteId;


                    contact.GivenName = givenName;
                    contact.FamilyName = familyName;

                    contact.DisplayName = remoteId;


                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(avatar));
                    HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;
                    MemoryStream memoryStream = new MemoryStream();
                    webResponse.GetResponseStream().CopyTo(memoryStream);
                    IRandomAccessStream stream = await ConvertToRandomAccessStream(memoryStream);

                    IDictionary<string, object> props = await contact.GetPropertiesAsync();
                    props.Add(KnownContactProperties.Nickname, username);
                    props.Add(KnownContactProperties.Url, url);




                    IDictionary<string, object> extprops = await contact.GetExtendedPropertiesAsync();
                    extprops.Add("Codename", username);
                    extprops.Add("ProfilePic", avatar);


                    await contact.SetDisplayPictureAsync(stream);

                    await contact.SaveAsync();

                }
                else
                {
                    StoredContact contact = await store.FindContactByRemoteIdAsync(remoteId);
                    IDictionary<string, object> extprops = await contact.GetExtendedPropertiesAsync();

                    if (!extprops.Values.Contains(avatar))
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(avatar));
                        HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;
                        MemoryStream memoryStream = new MemoryStream();
                        webResponse.GetResponseStream().CopyTo(memoryStream);
                        IRandomAccessStream stream = await ConvertToRandomAccessStream(memoryStream);
                        await contact.SetDisplayPictureAsync(stream);
                        extprops.Remove("ProfilePic");
                        extprops.Add("ProfilePic", avatar);

                        await contact.SaveAsync();
                    }
                }
            }
            catch (WebException) { }
        }

        public static async Task<IRandomAccessStream> ConvertToRandomAccessStream(MemoryStream memoryStream)
        {
            var randomAccessStream = new InMemoryRandomAccessStream();
            var outputStream = randomAccessStream.GetOutputStreamAt(0);
            var dw = new DataWriter(outputStream);
            var task = Task.Factory.StartNew(() => dw.WriteBytes(memoryStream.ToArray()));
            await task;
            await dw.StoreAsync();
            await outputStream.FlushAsync();
            return randomAccessStream;
        }


    }
}

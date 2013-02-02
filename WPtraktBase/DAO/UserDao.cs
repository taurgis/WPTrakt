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
using System.Threading.Tasks;
using WPtrakt.Model;
using WPtraktBase.Model.Trakt;

namespace WPtraktBase.DAO
{
    public class UserDao
    {
        public async Task<TraktLastActivity> FetchLastActivity()
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
    }
}

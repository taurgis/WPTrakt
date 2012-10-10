using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using WPtrakt.Model.Trakt;
using System.Runtime.Serialization.Json;
using System.IO;

namespace WPtrakt.Controllers
{
    public class StorageController
    {
        public static Boolean doesFileExist(String filename)
        {
            return IsolatedStorageFile.GetUserStoreForApplication().FileExists(filename);
        }


        public static TraktObject saveObject(TraktObject traktObject, Type type)
        {
            traktObject.DownloadTime = DateTime.Now;
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                if(!isoStore.DirectoryExists(traktObject.getFolder()))
                {
                    isoStore.CreateDirectory(traktObject.getFolder());
                }

                String fileName = traktObject.getFolder() + "/" + traktObject.getIdentifier() + ".json";
                using (var isoFileStream = isoStore.CreateFile(fileName))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(type);

                    ser.WriteObject(isoFileStream, traktObject);

                    isoFileStream.Close();
                }

            }

            return traktObject;
        }


        public static TraktObject LoadObject(String file, Type type)
        {  
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream stream = isoStore.OpenFile(file, FileMode.Open))
                {
                    var ser = new DataContractJsonSerializer(type);
                   return (TraktObject)ser.ReadObject(stream);
                }

            }
        }
    }
}

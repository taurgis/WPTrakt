using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using WPtrakt.Model.Trakt;

namespace WPtrakt.Controllers
{
    public class StorageController
    {
        public static Boolean doesFileExist(String filename)
        {
            try
            {
                return IsolatedStorageFile.GetUserStoreForApplication().FileExists(filename);
            }
            catch (IsolatedStorageException) {
                return false;
            }
        }

        public static void DeleteFile(String file)
        {
            try
            {
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(file);
            }
            catch (IsolatedStorageException) { }
        }

        public static TraktObject saveObject(TraktObject traktObject, Type type)
        {
            try
            {
                traktObject.DownloadTime = DateTime.Now;
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!isoStore.DirectoryExists(traktObject.getFolder()))
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
            catch (IsolatedStorageException) {
                return traktObject;
            }
        }

        public static TraktObject[] saveObject(TraktObject[] traktObject, Type type)
        {
            try
            {
                traktObject[0].DownloadTime = DateTime.Now;
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!isoStore.DirectoryExists(traktObject[0].getFolder()))
                    {
                        isoStore.CreateDirectory(traktObject[0].getFolder());
                    }

                    String fileName = traktObject[0].getFolder() + "/" + traktObject[0].getIdentifier() + ".json";
                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(type);

                        ser.WriteObject(isoFileStream, traktObject);

                        isoFileStream.Close();
                    }
                }

                return traktObject;
            }
            catch (IsolatedStorageException)
            {
                return traktObject;
            }
        }

        public static void saveObjectInMainFolder(Object theObject, Type type, String fileName)
        {
            try
            {
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(type);

                        ser.WriteObject(isoFileStream, theObject);

                        isoFileStream.Close();
                    }
                }
            }
            catch (IsolatedStorageException)
            { }
        }

        public static TraktObject LoadObject(String file, Type type)
        {
            try
            {
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream stream = isoStore.OpenFile(file, FileMode.Open))
                    {
                        var ser = new DataContractJsonSerializer(type);
                        TraktObject traktObject = (TraktObject)ser.ReadObject(stream);
                        stream.Close();

                        return traktObject;
                    }
                }
            }
            catch (IsolatedStorageException)
            {
                return null;
            }
        }

        public static TraktObject[] LoadObjects(String file, Type type)
        {
            try
            {
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream stream = isoStore.OpenFile(file, FileMode.Open))
                    {
                        var ser = new DataContractJsonSerializer(type);
                        TraktObject[] objects = (TraktObject[])ser.ReadObject(stream);

                        stream.Close();
                        return objects;
                    }
                }
            }
            catch (IsolatedStorageException)
            {
                return null;
            }
        }

        public static Object LoadObjectFromMain(String file, Type type)
        {
            try
            {
                using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream stream = isoStore.OpenFile(file, FileMode.Open))
                    {
                        var ser = new DataContractJsonSerializer(type);
                        Object tempObject = ser.ReadObject(stream);
                        stream.Close();
                        return tempObject;
                    }
                }
            }
            catch (IsolatedStorageException)
            {
                return null;
            }
        }
    }
}
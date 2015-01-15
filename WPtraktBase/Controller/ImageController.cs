using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WPtrakt.Model;

namespace WPtraktBase.Controllers
{
    public class ImageController
    {
        private static ImageController imageController;

        public static ImageController Instance
        {
            get
            {
                if (imageController == null)
                    imageController = new ImageController();

                return imageController;
            }
        }

        public static void copyImageToShellContent(String filename, String uniquekey)
        {
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!StorageController.doesFileExist("/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg"))
                    {
                        IsolatedStorageFileStream stream = store.OpenFile(filename, FileMode.Open);
                        saveImageForTile("/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg", stream, 1920, 100);
                        stream.Dispose();
                        stream.Close();
                    }
                }
            }
            catch (IsolatedStorageException) { }
        }


        public static BitmapImage getImageFromStorage(String filename)
        {
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    BitmapImage bi = new BitmapImage();

                    using (IsolatedStorageFileStream stream = store.OpenFile(filename, FileMode.Open))
                    {
                        bi.DecodePixelType = DecodePixelType.Logical;
                        bi.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                        bi.CreateOptions = BitmapCreateOptions.DelayCreation; 
                        bi.SetSource(stream);
                        stream.Dispose();
                        stream.Close();
                    }
                  
                    return bi;
                }
            }
            catch (IsolatedStorageException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static BitmapImage saveImage(String fileName, Stream pic, Int16 width, Int16 height, Int16 quality)
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var bi = new BitmapImage();
                bi.SetSource(pic);
                try
                {
                    var wb = new WriteableBitmap(bi);

                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        wb.SaveJpeg(isoFileStream, width, height, 0, quality);
                        bi.SetSource(isoFileStream);
                        isoFileStream.Dispose();
                        isoFileStream.Close();
                        wb = null;
                    }
                }
                catch (IsolatedStorageException){ }
                pic.Close();
                return bi;
            }
        }


        public static BitmapImage saveImage(String fileName, Stream pic, Int16 width, Int16 quality)
        {

            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var bi = new BitmapImage();
                bi.SetSource(pic);
                bi.DecodePixelWidth = width;
                try
                {
                    var wb = new WriteableBitmap(bi);

                    double newHeight = wb.PixelHeight * ((double)width / wb.PixelWidth);

                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        wb.SaveJpeg(isoFileStream, width, (int)newHeight, 0, quality);
                        bi.SetSource(isoFileStream);
                        isoFileStream.Dispose();
                        isoFileStream.Close();
                        wb = null;
                    }
                }
                catch (IsolatedStorageException)
                { }

                return bi;
            }
        }

        public static async Task<BitmapImage> FetchImageFromUrl(String fanartUrl, String fileName, short width)
        {
            Debug.WriteLine("Fetching from " + fanartUrl + " and saving to " + fileName + ".");
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(fanartUrl));
                HttpWebResponse webResponse = await request.GetResponseAsync() as HttpWebResponse;

                System.Net.HttpStatusCode status = webResponse.StatusCode;

                if (status == System.Net.HttpStatusCode.OK)
                {
                    Stream str = webResponse.GetResponseStream();
                    return ImageController.saveImage(fileName, str, width, 100);
                }
            }
            catch (WebException) { }
            catch (TargetInvocationException) { }

            return new BitmapImage();
        }

        public static void saveImageForTile(String fileName, Stream pic, Int16 width, Int16 quality)
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var bi = new BitmapImage();
                bi.SetSource(pic);

                try
                {
                    var wb = new WriteableBitmap(bi);

                    double newHeight = wb.PixelHeight * ((double)width / wb.PixelWidth);

                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        wb.SaveJpeg(isoFileStream, width, (int)newHeight, 0, quality);
                        isoFileStream.Dispose();
                        isoFileStream.Close();
                        wb = null;
                        bi = null;
                    }
                }
                catch (IsolatedStorageException)
                { }
            }
        }
    }
}
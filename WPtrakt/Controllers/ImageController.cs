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
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using WPtrakt.Model;

namespace WPtrakt.Controllers
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
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!StorageController.doesFileExist("/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg"))
                {
                    store.CopyFile(filename, "/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg");
                }
            }
        }

       
        public static BitmapImage getImageFromStorage(String filename)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                BitmapImage bi = new BitmapImage();
                try
                {
                    using (IsolatedStorageFileStream stream = store.OpenFile(filename, FileMode.Open))
                    {
                        bi.SetSource(stream);
                    }
                    return bi;
                }
                catch (IsolatedStorageException)
                {
                    return null;
                }
                catch(ArgumentException)
                {
                    return null;
                }
            }
        }

        public static BitmapImage saveImage(String fileName, Stream pic, Int16 width, Int16 height, Int16 quality)
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var bi = new BitmapImage();
                bi.SetSource(pic);

                if (!AppUser.UserIsHighEndDevice())
                    return bi;

                try
                {

                    var wb = new WriteableBitmap(bi);

                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        try
                        {
                            Extensions.SaveJpeg(wb, isoFileStream, width, height, 0, quality);
                        }
                        catch (IsolatedStorageException)
                        {
                            //Do nothing for now.
                        }
                    }
                }
                catch (IsolatedStorageException) {  }

               return bi;
            }
        }

   
    }
}

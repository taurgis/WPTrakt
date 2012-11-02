using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPtrakt.Model;
using System.Linq;

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
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!StorageController.doesFileExist("/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg"))
                    {
                        IsolatedStorageFileStream stream = store.OpenFile(filename, FileMode.Open);
                        saveImage("/Shared/ShellContent/wptraktbg" + uniquekey + ".jpg", stream, 800, 450, 100);
                        stream.Close();
                    }
                }
            }
            catch (IsolatedStorageException) { }
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

                        stream.Close();
                    }
                    return bi;
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
        }

        public static BitmapImage saveImage(String fileName, Stream pic, Int16 width, Int16 height, Int16 quality)
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var bi = new BitmapImage();
                bi.SetSource(pic);

                if (!AppUser.UserIsHighEndDevice())
                {
                    BitmapImage lowEndDeviceImage = resizeImage(bi, pic, width, height);
                    pic.Close();
                    return lowEndDeviceImage;
                }

                try
                {

                    var wb = new WriteableBitmap(bi);

                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        try
                        {
                            Extensions.SaveJpeg(wb, isoFileStream, width, height, 0, quality);
                            isoFileStream.Close();
                        }
                        catch (IsolatedStorageException)
                        {
                            //Do nothing for now.
                        }
                    }
                }
                catch (IsolatedStorageException)
                {
                }
                pic.Close();
                return bi;
            }
        }

        private static BitmapImage resizeImage(BitmapImage bi, Stream stream, int width, int height)
        {
            Image resizedImage = new Image();
            WriteableBitmap bitmap = new WriteableBitmap(resizedImage, null);
            BitmapImage bmp = new BitmapImage();
            bitmap.SetSource(stream);

            double newHeight = bitmap.PixelHeight * ((double)width / bitmap.PixelWidth);
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.SaveJpeg(ms, width, height, 0, 80);
                bmp.SetSource(ms);
                ms.Close();
            }
            return bmp;
        }

        private static BitmapImage resizeImage(BitmapImage bi, Stream stream, int width)
        {
            Image resizedImage = new Image();
            WriteableBitmap bitmap = new WriteableBitmap(resizedImage, null);
            BitmapImage bmp = new BitmapImage();
            bitmap.SetSource(stream);
            using (MemoryStream ms = new MemoryStream())
            {
                double newHeight = bitmap.PixelHeight * ((double)width / bitmap.PixelWidth);
                bitmap.SaveJpeg(ms, width, (int)newHeight, 0, 80);
                bmp.SetSource(ms);

            }
            return bmp;
        }



        public static BitmapImage saveImage(String fileName, Stream pic, Int16 width, Int16 quality)
        {
            using (var isoStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var bi = new BitmapImage();
                bi.SetSource(pic);

                if (!AppUser.UserIsHighEndDevice())
                {
                    BitmapImage lowEndDeviceImage = resizeImage(bi, pic, width);
                    pic.Close();
                    return lowEndDeviceImage;
                }

                try
                {

                    var wb = new WriteableBitmap(bi);

                    double newHeight = wb.PixelHeight * ((double)width / wb.PixelWidth);

                    using (var isoFileStream = isoStore.CreateFile(fileName))
                    {
                        try
                        {
                            Extensions.SaveJpeg(wb, isoFileStream, width, (int)newHeight, 0, quality);
                            isoFileStream.Close();
                        }
                        catch (IsolatedStorageException)
                        {
                            //Do nothing for now.
                        }
                    }
                }
                catch (IsolatedStorageException)
                {
                }

                return bi;
            }
        }


    }
}

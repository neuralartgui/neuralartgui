using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeepdreamGui.Model
{
    /// <summary>
    /// Class to perform image and imagefile manipulation
    /// </summary>
    public static class ImageModel
    {
        /// <summary>
        /// Check if file is locked by another process
        /// </summary>
        /// <param name="file">Filename</param>
        /// <returns>File is locked</returns>
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException ex)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                Trace.WriteLine("ImageModel.IsFileLocked " + ex);
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }

        /// <summary>
        /// Get Task that blocks while file is locked
        /// </summary>
        /// <param name="file">Filename</param>
        /// <param name="maxTime">Time to wait for file to be released</param>
        /// <returns></returns>
        private static Task BlockWhileFileIsLocked(string file, TimeSpan maxTime)
        {
            return Task.Factory.StartNew(() =>
            {
                while (IsFileLocked(new FileInfo(file)) && maxTime.Ticks > 0)
                {
                    System.Threading.Thread.Sleep(100);
                    maxTime = maxTime - TimeSpan.FromMilliseconds(100);
                }
            });
        }

        /// <summary>
        /// Load Image from File
        /// </summary>
        /// <param name="path">Path to imagefile</param>
        /// <param name="deleteAfterLoad">Optional: Delete File after load has been performed</param>
        /// <returns></returns>
        public static BitmapImage LoadImage(string path, bool deleteAfterLoad = false)
        {
            var t = LoadImageAsync(path, deleteAfterLoad);
            if (t == null) return null;
            t.Wait();
            return t.Result;
        }

        /// <summary>
        /// Load Image asynchronously.
        /// </summary>
        /// <param name="path">Path to Imagefile</param>
        /// <param name="deleteAfterLoad">Optional: Delete File after load has been performed</param>
        /// <returns>Task that loads image</returns>
        public static Task<BitmapImage> LoadImageAsync(string path, bool deleteAfterLoad = false)
        {
            if (String.IsNullOrEmpty(path)) return null;
            if (!File.Exists(path)) return null;
            return Task<BitmapImage>.Factory.StartNew(() =>
            {
                try
                {
                    BlockWhileFileIsLocked(path, TimeSpan.FromSeconds(2)).Wait();
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    img.UriSource = new Uri(path);
                    img.EndInit();
                    img.Freeze();

                    if (!deleteAfterLoad) return img;

                    Task.Factory.StartNew(() =>
                    {
                        BlockWhileFileIsLocked(path, TimeSpan.FromSeconds(10)).Wait();
                        Worker.DeleteFile(path);
                    });
                    return img;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(@"ImageModel.LoadImageAsync; " + ex);
                    return null;
                }
            });
        }

        /// <summary>
        /// Render Canvas over Image
        /// </summary>
        /// <param name="source">Source Canvas</param>
        /// <returns>Task that returns a combined image of canvas and workImage</returns>
        public static BitmapImage RenderCanvas(Canvas source)
        {
            try
            {
                //Get all needed Images and Controls
                Image workImage = source.Children.OfType<Image>().FirstOrDefault();
                var stampControl = source.Children.OfType<ContentControl>().FirstOrDefault();
                var stampImage = (System.Windows.Controls.Image) stampControl?.Content;

                if (workImage == null || stampControl == null || stampImage == null) return null;

                var baseImage = workImage.Source as BitmapImage;
                if (baseImage == null) return null;

                var waterImage = stampImage.Source as BitmapImage;
                if (waterImage == null) return null;

                //Offset of Stamp in ContentControl
                var innerOffset = stampImage.TransformToAncestor(stampControl).Transform(new Point(0, 0));

                //Base Image in Canvas Offset
                var baseImageOffset = workImage.TransformToAncestor(source).Transform(new Point(0, 0));

                //Scale Factor (Only one direction, because Image keeps aspect on resize)
                double scalingX =  baseImage.PixelWidth / workImage.ActualWidth;


                //Add base image Offset, since its already negated
                double topStamp = Canvas.GetTop(stampControl) + innerOffset.Y;
                double leftStamp = Canvas.GetLeft(stampControl) + innerOffset.X;

                //Offset of Stamp to Base Image
                double stampToBaseOffsetX = (leftStamp - baseImageOffset.X) * scalingX;
                double stampToBaseOffsetY = (topStamp - baseImageOffset.Y) * scalingX;

                //Create new Canvas to Render Image in original Size
                var creatorCanvas = new Canvas()
                {
                    Width = baseImage.PixelWidth,
                    Height = baseImage.PixelHeight,
                    Background = new ImageBrush(baseImage)
                };

                //Create new Image Control for Watermark with proper Size
                var stampImageControl = new Image() { Source = waterImage, Width=stampImage.ActualWidth * scalingX, Height=stampImage.ActualHeight * scalingX, Opacity= stampImage.Opacity};
                stampImageControl.RenderTransformOrigin = new Point(0.5,0.5);

                //Apply Rotation if available
                if(stampControl.RenderTransform != null && stampControl.RenderTransform is RotateTransform)
                {
                    stampImageControl.RenderTransform = new RotateTransform(((RotateTransform)stampControl.RenderTransform).Angle);
                }

                //Set Watermark Position on new Canvas
                Canvas.SetLeft(stampImageControl, stampToBaseOffsetX);
                Canvas.SetTop(stampImageControl, stampToBaseOffsetY);
                creatorCanvas.Children.Add(stampImageControl);

                //This is somehow important
                Size sourceSize = new Size(creatorCanvas.Width, creatorCanvas.Height);
                creatorCanvas.Measure(sourceSize);
                creatorCanvas.Arrange(new Rect(sourceSize));

                //Render Canvas to Bitmap Image
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)sourceSize.Width, (int)sourceSize.Height,
                    96d,96d, PixelFormats.Default);
                renderBitmap.Render(creatorCanvas);

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                BitmapImage tmpBitMap = new BitmapImage();
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    tmpBitMap.BeginInit();
                    tmpBitMap.CacheOption = BitmapCacheOption.OnLoad;
                    tmpBitMap.StreamSource = stream;
                    tmpBitMap.EndInit();
                }
                return tmpBitMap;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("ImageModel.RenderCanvas; " + ex);
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Save Image to path with globally unique id as name
        /// </summary>
        /// <param name="img">Image</param>
        /// <param name="path">Path to save image</param>
        /// <returns>Task that svaes image</returns>
        public static Task<string> SaveImageWithRandomName(BitmapSource img, string path)
        {
            return Task<string>.Factory.StartNew(() =>
            {
                var guid = Guid.NewGuid();
                var imgPath = Path.Combine(path, guid + ".jpg");
                Trace.Write("Save picture to=" + imgPath);
                SaveImage(img, imgPath).Wait();
                return imgPath;
            });
        }

        /// <summary>
        /// Save Image to Path
        /// </summary>
        /// <param name="img">Image</param>
        /// <param name="path">Path to save image. Should end with .jpg</param>
        /// <returns></returns>
        public static Task SaveImage(BitmapSource img, string path)
        {
            return Task.Factory.StartNew(() =>
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = 100 };
                if (!path.EndsWith(".jpg"))
                    path += ".jpg";

                encoder.Frames.Add(BitmapFrame.Create(img));
                try
                {
                    Trace.WriteLine("Save picture to=" + path);
                    using (var filestream = new FileStream(path, FileMode.Create))
                        encoder.Save(filestream);
                }
                catch (Exception ex)
                {

                    Trace.WriteLine("ImageModel.SaveImage; " + ex);
                    //Console.WriteLine(ex);
                }
            });
        }

        // http://stackoverflow.com/questions/26307116/ios-rotate-image-and-zoom-so-it-fits-original-frame-like-in-instagram
        /// <summary>
        /// Calculate min Scaling of Image based on rotation angle, to prevent "black edges".
        /// </summary>
        /// <param name="Width">Image Width</param>
        /// <param name="Height">Image Height</param>
        /// <param name="angle">Rotation Angle</param>
        /// <returns>Min Scale Factor</returns>
        public static double ScaleToFill(double Width, double Height, double angle)
        {
            angle = Math.Abs(angle);
            angle = ((angle * 2 * Math.PI) / 360);

            var theta = angle;
            double scale1 = (Height * Math.Cos(theta) + Width * Math.Sin(theta)) / Height;
            double scale2 = (Height * Math.Sin(theta) + Width * Math.Cos(theta)) / Width;
            double scaleFactor = Math.Abs(Math.Max(scale1, scale2));
            scaleFactor *= 100;
            scaleFactor = Math.Ceiling(scaleFactor);
            return scaleFactor;
        }
    }
}
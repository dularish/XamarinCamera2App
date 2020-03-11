using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
//using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;
using Java.IO;
using Java.Nio;

namespace CameraApp.Droid.Camera.Listeners
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        bool _isFirstFrameProcessed = false;
        private Context _context;
        private int index = 0;

        public ImageAvailableListener(Context context)
        {
            this._context = context;
        }

        public void OnImageAvailable(ImageReader reader)
        {
            Image image = reader.AcquireLatestImage();

            if (image != null && index < 10)
            {
                Java.IO.File file = new Java.IO.File(_context.GetExternalFilesDir(null), "sampleImage" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + index++ + ".jpeg");
                byte[] nv21bytes = YUV420toNV21(image);
                byte[] bytes = NV21toJPEG(nv21bytes, image.Width, image.Height, 100);
                using (var output = new FileOutputStream(file))
                {
                    try
                    {
                        output.Write(bytes);
                    }
                    catch (Java.IO.IOException e)
                    {
                        e.PrintStackTrace();
                    }
                    catch(Exception ex)
                    {

                    }
                }

                _isFirstFrameProcessed = true;
            }

            image?.Close();
        }

        /// <summary>
        /// Copied from https://stackoverflow.com/a/45926852
        /// </summary>
        /// <param name="nv21"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        private static byte[] NV21toJPEG(byte[] nv21, int width, int height, int quality)
        {
            System.IO.MemoryStream outStream = new System.IO.MemoryStream();
            YuvImage yuv = new YuvImage(nv21, ImageFormat.Nv21, width, height, null);
            yuv.CompressToJpeg(new Rect(0, 0, width, height), quality, outStream);
            return outStream.ToArray();
        }

        /// <summary>
        /// Copied from https://stackoverflow.com/a/45926852
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static byte[] YUV420toNV21(Image image)
        {
            Rect crop = image.CropRect;
            ImageFormatType format = image.Format;
            int width = crop.Width();
            int height = crop.Height();
            Image.Plane[] planes = image.GetPlanes();
            byte[] data = new byte[width * height * ImageFormat.GetBitsPerPixel(format) / 8];
            byte[] rowData = new byte[planes[0].RowStride];

            int channelOffset = 0;
            int outputStride = 1;
            for (int i = 0; i < planes.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        channelOffset = 0;
                        outputStride = 1;
                        break;
                    case 1:
                        channelOffset = width * height + 1;
                        outputStride = 2;
                        break;
                    case 2:
                        channelOffset = width * height;
                        outputStride = 2;
                        break;
                }

                ByteBuffer buffer = planes[i].Buffer;
                int rowStride = planes[i].RowStride;
                int pixelStride = planes[i].PixelStride;

                int shift = (i == 0) ? 0 : 1;
                int w = width >> shift;
                int h = height >> shift;
                buffer.Position(rowStride * (crop.Top >> shift) + pixelStride * (crop.Left >> shift));
                for (int row = 0; row < h; row++)
                {
                    int length;
                    if (pixelStride == 1 && outputStride == 1)
                    {
                        length = w;
                        buffer.Get(data, channelOffset, length);
                        channelOffset += length;
                    }
                    else
                    {
                        length = (w - 1) * pixelStride + 1;
                        buffer.Get(rowData, 0, length);
                        for (int col = 0; col < w; col++)
                        {
                            data[channelOffset] = rowData[col * pixelStride];
                            channelOffset += outputStride;
                        }
                    }
                    if (row < h - 1)
                    {
                        buffer.Position(buffer.Position() + rowStride - length);
                    }
                }
            }
            return data;
        }
    }

    
}
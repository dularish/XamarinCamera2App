using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using CameraApp.CameraView;
using Java.Interop;
using Java.IO;
using Java.Nio;
using MathNet.Numerics.LinearAlgebra;
using OpenCvSharp;

namespace CameraApp.Droid.Camera.Listeners
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        private Context _context;
        private Action<string> _predictionGeneratedCallback;
        private ImageProcessingMode _imageProcessingMode;
        private NeuralNetwork.Model _neuralNetModel;

        public ImageAvailableListener(Context context, Action<string> predictionGenCallback, ImageProcessingMode imageProcessingMode)
        {
            this._context = context;
            _predictionGeneratedCallback = predictionGenCallback;
            _imageProcessingMode = imageProcessingMode;
            switch (imageProcessingMode)
            {
                case ImageProcessingMode.JustPreview:
                    break;
                case ImageProcessingMode.CatsDetection:
                    var assembly = IntrospectionExtensions.GetTypeInfo(this.GetType()).Assembly;
                    var stream = assembly.GetManifestResourceStream("CameraApp.Droid.MLDependencies.Models.CatsDetectionModel.xml");
                    _neuralNetModel = HelperFunctionsForML.deserializeModelFromStream<NeuralNetwork.Model>(stream);
                    break;
                default:
                    break;
            }
        }

        public void OnImageAvailable(ImageReader reader)
        {
            Image image = reader.AcquireLatestImage();

            if (image != null && _imageProcessingMode != ImageProcessingMode.JustPreview)
            {
                Mat rgbMat = getRGBMatMethod1(image);
                Mat subMat = squareTheMatrix(rgbMat);
                Mat subMatRotated = rotateMatrix90DegCCW(subMat);
                Mat resizedMat = resizeMat(subMatRotated, 64, 64);
                byte[] subMatRotatedBytes = getAllPixelBytes(resizedMat);
                //Use the above bytes for input to Machine Learning
                var imageMat = Matrix<double>.Build.DenseOfRowArrays(new double[][] { subMatRotatedBytes.Select(s => (double)s).ToArray() });
                imageMat = imageMat / 255.0;
                imageMat = imageMat.Transpose();
                if(_neuralNetModel != null)
                {
                    var preds = NeuralNetwork.predictClass(imageMat, _neuralNetModel.TrainedParams);
                    _predictionGeneratedCallback.Invoke(preds[0, 0].ToString());
                }
            }

            image?.Close();
        }

        private static Mat resizeMat(Mat subMatRotated, int rows, int cols)
        {
            return subMatRotated.Resize(new Size(cols, rows));
        }

        [Obsolete("Logic looked promising but doesn't work, dev must spend more time on this when available")]
        /// <summary>
        /// Copied from https://answers.opencv.org/question/61628/android-camera2-yuv-to-rgb-conversion-turns-out-green/?answer=100322#post-id-100322
        /// </summary>
        /// <param name="image"></param>
        /// <param name="isGreyOnly"></param>
        /// <returns></returns>
        public static Mat convertYuv420888ToRGBMatMethod2(Image image, bool isGreyOnly)
        {
            int width = image.Width;
            int height = image.Height;

            Image.Plane yPlane = image.GetPlanes()[0];
            int ySize = yPlane.Buffer.Remaining();

            if (isGreyOnly)
            {
                byte[] grayData = new byte[ySize];
                yPlane.Buffer.Get(grayData, 0, ySize);

                Mat greyMat = new Mat(height, width, MatType.CV_8UC1);
                greyMat.SetArray(0, 0, grayData);

                return greyMat;
            }

            Image.Plane uPlane = image.GetPlanes()[1];
            Image.Plane vPlane = image.GetPlanes()[2];

            // be aware that this size does not include the padding at the end, if there is any
            // (e.g. if pixel stride is 2 the size is ySize / 2 - 1)
            int uSize = uPlane.Buffer.Remaining();
            int vSize = vPlane.Buffer.Remaining();

            byte[] data = new byte[ySize + (ySize / 2)];

            yPlane.Buffer.Get(data, 0, ySize);

            ByteBuffer ub = uPlane.Buffer;
            ByteBuffer vb = vPlane.Buffer;

            int uvPixelStride = uPlane.PixelStride; //stride guaranteed to be the same for u and v planes
            if (uvPixelStride == 1)
            {
                uPlane.Buffer.Get(data, ySize, uSize);
                vPlane.Buffer.Get(data, ySize + uSize, vSize);

                Mat yuvMatStride1 = new Mat(height + (height / 2), width, MatType.CV_8UC1);
                yuvMatStride1.SetArray(0, 0, data);
                Mat rgbMatStride1 = new Mat(height, width, MatType.CV_8UC3);
                rgbMatStride1 = yuvMatStride1.CvtColor(ColorConversionCodes.YUV2RGBA_I420, 3);
                yuvMatStride1.Release();
                return rgbMatStride1;
            }

            // if pixel stride is 2 there is padding between each pixel
            // converting it to NV21 by filling the gaps of the v plane with the u values
            vb.Get(data, ySize, vSize);
            for (int i = 0; i < uSize; i += 2)
            {
                data[ySize + i + 1] = (byte)((int)ub.Get(i) + 128);
            }

            Mat yuvMat = new Mat(height + (height / 2), width, MatType.CV_8UC1);
            yuvMat.SetArray(0, 0, data);
            Mat rgbMat = new Mat(height, width, MatType.CV_8UC3);
            rgbMat = yuvMat.CvtColor(ColorConversionCodes.YUV2RGB_NV21, 3);
            yuvMat.Release();
            return rgbMat;
        }

        /// <summary>
        /// Gets byte array which can be directly saved as .jpeg, and can be viewed by any image viewer
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static byte[] getJpegForYuv420888(Image image)
        {
            byte[] nv21bytes = YUV420toNV21(image);
            byte[] jpegBytes = NV21toJPEG(nv21bytes, image.Width, image.Height, 100);
            return jpegBytes;
        }

        private static Mat squareTheMatrix(Mat mat)
        {
            return mat.SubMat(0, Math.Min(mat.Rows, mat.Cols), 0, Math.Min(mat.Rows, mat.Cols));
        }

        /// <summary>
        /// Gets all the individual pixels, which can be used for machine learning purposes, cannot be viewed
        /// by most popular image viewer apps when saved to .jpeg or .png
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        private static byte[] getAllPixelBytes(Mat mat)
        {
            Vec3b[] vec3bData = new Vec3b[mat.Rows * mat.Cols];
            mat.GetArray(0, 0, vec3bData);

            byte[] outputBytes = new byte[vec3bData.Length * 3];
            for (int i = 0; i < vec3bData.Length; i++)
            {
                outputBytes[i * 3] = vec3bData[i].Item0;
                outputBytes[i * 3 + 1] = vec3bData[i].Item1;
                outputBytes[i * 3 + 2] = vec3bData[i].Item2;
            }

            return outputBytes;
        }

        private static Mat rotateMatrix90DegCCW(Mat mat)
        {
            if(mat.Rows != mat.Cols)
            {
                throw new NotImplementedException("To add more logic and test more for rectangular images");
            }
            Point2f sourceCenter = new Point2f((float)(mat.Cols / 2.0), (float)(mat.Rows / 2.0));
            Mat rotationMatrix = Cv2.GetRotationMatrix2D(sourceCenter, 90, 1.0);
            Mat rotatedMat = new Mat(mat.Height, mat.Width, mat.Type());
            Cv2.WarpAffine(mat, rotatedMat, rotationMatrix, mat.Size());
            return rotatedMat;
        }

        private static Mat getRGBMatMethod1(Image image)
        {
            var yuvMat = getYuvMatForImage(image);
            
            var bgrMatConv = yuvMat.CvtColor(ColorConversionCodes.YUV2BGRA_I420);
            var rgbMat = bgrMatConv.CvtColor(ColorConversionCodes.BGRA2RGB);
            
            return rgbMat;
        }

        /// <summary>
        /// Copied from https://stackoverflow.com/a/35221548
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Mat getYuvMatForImage(Image image)
        {
            ByteBuffer buffer;
            int rowStride;
            int pixelStride;
            int width = image.Width;
            int height = image.Height;
            int offset = 0;

            Image.Plane[] planes = image.GetPlanes();
            byte[] data = new byte[image.Width * image.Height * ImageFormat.GetBitsPerPixel(ImageFormatType.Yuv420888) / 8];
            byte[] rowData = new byte[planes[0].RowStride];

            for (int i = 0; i < planes.Length; i++)
            {
                buffer = planes[i].Buffer;
                rowStride = planes[i].RowStride;
                pixelStride = planes[i].PixelStride;
                int w = (i == 0) ? width : width / 2;
                int h = (i == 0) ? height : height / 2;
                for (int row = 0; row < h; row++)
                {
                    int bytesPerPixel = ImageFormat.GetBitsPerPixel(ImageFormatType.Yuv420888) / 8;
                    if (pixelStride == bytesPerPixel)
                    {
                        int length = w * bytesPerPixel;
                        buffer.Get(data, offset, length);

                        if (h - row != 1)
                        {
                            buffer.Position(buffer.Position() + rowStride - length);
                        }
                        offset += length;
                    }
                    else
                    {


                        if (h - row == 1)
                        {
                            buffer.Get(rowData, 0, width - pixelStride + 1);
                        }
                        else
                        {
                            buffer.Get(rowData, 0, rowStride);
                        }

                        for (int col = 0; col < w; col++)
                        {
                            data[offset++] = rowData[col * pixelStride];
                        }
                    }
                }
            }

            Mat mat = new Mat(height + height / 2, width, MatType.CV_8UC1);
            mat.SetArray(0, 0, data);

            return mat;
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
            yuv.CompressToJpeg(new Android.Graphics.Rect(0, 0, width, height), quality, outStream);
            return outStream.ToArray();
        }

        /// <summary>
        /// Copied from https://stackoverflow.com/a/45926852
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static byte[] YUV420toNV21(Image image)
        {
            Android.Graphics.Rect crop = image.CropRect;
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
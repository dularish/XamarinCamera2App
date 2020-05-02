using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Nio;
using Java.Nio.Channels;
using OpenCvSharp;
using Xamarin.TensorFlow.Lite;

namespace CameraApp.Droid.MachineLearning.Tensorflow
{
    public class TensorflowImageClassifier
    {
        private Interpreter _Interpreter;
        private List<string> _Labels;
        private ByteBuffer _ByteBuffer;
        private float[][] _OutputLocations;
        private Java.Lang.Object _Outputs;
        private const int _DataUnitSize = 4;//Size of float

        public TensorflowImageClassifier(AssetFileDescriptor assetFileDescriptor, List<string> labels)
        {
            var inputStream = new FileInputStream(assetFileDescriptor.FileDescriptor);
            var mappedByteBuffer = inputStream.Channel.Map(FileChannel.MapMode.ReadOnly, assetFileDescriptor.StartOffset, assetFileDescriptor.DeclaredLength);

            _Interpreter = new Interpreter(mappedByteBuffer);
            var inputTensor = _Interpreter.GetInputTensor(0);
            var inputTensorShape = inputTensor.Shape();
            ImageHeight = inputTensorShape[1];
            ImageWidth = inputTensorShape[2];
            PixelSize = inputTensorShape[3];
            _Labels = labels;

            var imageInputSize = _DataUnitSize * PixelSize * ImageHeight * ImageWidth;
            _ByteBuffer = ByteBuffer.AllocateDirect(imageInputSize);
            _ByteBuffer.Order(ByteOrder.NativeOrder());

            _OutputLocations = new float[1][] { new float[_Labels.Count] };
            _Outputs = Java.Lang.Object.FromArray(_OutputLocations);

            //testMnistDataset();
        }

        private void testMnistDataset()
        {
            var streamReader = new StreamReader(Application.Context.Assets.Open("mnist_testing_dataset.csv"));

            var allRows = streamReader.ReadToEnd().Split('\n');

            var trueValuePredsTuple = new List<(string, string)>();

            for (int i = 0; i < allRows.Length; i++)
            {
                var allElements = allRows[i].Split(',');

                var trueValue = allElements[0];

                _ByteBuffer.Rewind();

                for (int j = 0; j < ImageHeight * ImageHeight; j++)
                {
                    _ByteBuffer.PutFloat((float)(float.Parse(allElements[j+1]) / 255.0));
                }

                _Interpreter.Run(_ByteBuffer, _Outputs);

                var floatArray = _Outputs.ToArray<float[]>();
                var predictions = floatArray[0];
                int indexPred = predictions.Select((value, index) => new { Value = value, Index = index }).Aggregate((item1, item2) => item1.Value > item2.Value ? item1 : item2).Index;

                trueValuePredsTuple.Add((trueValue, _Labels[indexPred]));
            }

        }

        public int ImageHeight { get; private set; }
        public int ImageWidth { get; private set; }

        public int PixelSize { get; private set; }

        internal string Classify(Mat imageMat)
        {
            _ByteBuffer.Rewind();
            
            if(imageMat.Type() == MatType.CV_8UC1 && PixelSize == 1)
            {
                for (int i = 0; i < ImageHeight; i++)
                {
                    for (int j = 0; j < ImageWidth; j++)
                    {
                        _ByteBuffer.PutFloat((float)((float)imageMat.Get<byte>(i, j) / 255.0));
                    }
                }
            }
            else if(imageMat.Type() == MatType.CV_8UC3 && PixelSize == 3)
            {
                Vec3b[] vec3bData = new Vec3b[imageMat.Rows * imageMat.Cols];
                imageMat.GetArray(0, 0, vec3bData);

                for (int i = 0; i < ImageHeight; i++)
                {
                    for (int j = 0; j < ImageWidth; j++)
                    {
                        _ByteBuffer.PutFloat((float)((float)vec3bData[i].Item0 / 255.0));
                        _ByteBuffer.PutFloat((float)((float)vec3bData[i].Item1 / 255.0));
                        _ByteBuffer.PutFloat((float)((float)vec3bData[i].Item2 / 255.0));
                    }
                }
            }
            
            _Interpreter.Run(_ByteBuffer, _Outputs);

            var floatArray = _Outputs.ToArray<float[]>();
            var predictions = floatArray[0];
            int indexPred = predictions.Select((value, index) => new { Value = value, Index = index }).Aggregate((item1, item2) => item1.Value > item2.Value ? item1 : item2).Index;

            return _Labels[indexPred];
        }
    }
}
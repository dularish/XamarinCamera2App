using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace CameraApp.CameraView
{
    public class CameraView : View
    {
        public CameraOptions Camera
        {
            get { return (CameraOptions)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public static readonly BindableProperty CameraProperty =
            BindableProperty.Create("Camera", typeof(CameraOptions), typeof(CameraView), CameraOptions.Rear);

        public string ComputerVisionPrediction
        {
            get { return (string)GetValue(ComputerVisionPredictionProperty); }
            set { SetValue(ComputerVisionPredictionProperty, value); }
        }

        public static readonly BindableProperty ComputerVisionPredictionProperty =
            BindableProperty.Create("ComputerVisionPrediction", typeof(string), typeof(CameraView), "");



        public ImageProcessingMode ImageProcessingMode
        {
            get { return (ImageProcessingMode)GetValue(ImageProcessingModeProperty); }
            set { SetValue(ImageProcessingModeProperty, value); }
        }

        
        public static readonly BindableProperty ImageProcessingModeProperty =
            BindableProperty.Create("ImageProcessingMode", typeof(ImageProcessingMode), typeof(CameraView), ImageProcessingMode.JustPreview);


    }
}

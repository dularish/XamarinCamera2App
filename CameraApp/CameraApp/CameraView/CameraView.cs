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

    }
}

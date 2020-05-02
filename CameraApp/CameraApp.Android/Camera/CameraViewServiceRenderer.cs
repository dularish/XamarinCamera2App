using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using CameraApp.CameraView;
using CameraApp.Droid.Camera;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewServiceRenderer))]
namespace CameraApp.Droid.Camera
{
    public class CameraViewServiceRenderer : ViewRenderer<CameraView.CameraView, CameraDroid>
    {
        private Context _context;
        private CameraDroid _camera;

        public CameraViewServiceRenderer(Context context) : base(context)
        {
            _context = context;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CameraView.CameraView> e)
        {
            base.OnElementChanged(e);

            bool isPermissionAvailable = getCameraPermissionStatus();
            _camera = new CameraDroid(Context);
            _camera.PredictionUpdated += ((sender,newPrediction) => 
            {
                if(e.NewElement != null)
                {
                    e.NewElement.ComputerVisionPrediction = newPrediction;
                }
            });
            _camera.ProcessedImagePreviewUpdated += ((sender, newImageSource) =>
            {
                if (e.NewElement != null)
                {
                    e.NewElement.ProcessedImagePreview = newImageSource;
                }
            });
            CameraOptions cameraOption = e.NewElement?.Camera ?? CameraOptions.Rear;

            if(Control == null)
            {
                if (isPermissionAvailable)
                {
                    _camera.OpenCamera(cameraOption, e.NewElement.ImageProcessingMode);
                    SetNativeControl(_camera);
                }
                else
                {
                    MainActivity.CameraPermissionsGranted += (s, args) =>
                    {
                        _camera.OpenCamera(cameraOption, e.NewElement.ImageProcessingMode);
                        SetNativeControl(_camera);
                    };
                }
            }
        }

        private bool getCameraPermissionStatus()
        {
            const string permission = Manifest.Permission.Camera;

            if((int)Build.VERSION.SdkInt < 23 || ContextCompat.CheckSelfPermission(Android.App.Application.Context, permission) == Android.Content.PM.Permission.Granted)
            {
                return true;
            }

            ActivityCompat.RequestPermissions((MainActivity)_context, MainActivity.CameraPermissions, MainActivity.CameraPermissionsCode);

            return false;
        }
    }
}
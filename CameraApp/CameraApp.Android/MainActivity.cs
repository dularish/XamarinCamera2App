using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android;
using Android.Support.Design.Widget;

namespace CameraApp.Droid
{
    [Activity(Label = "CameraApp", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public const int CameraPermissionsCode = 1;

        public static readonly string[] CameraPermissions =
        {
            Manifest.Permission.Camera
        };

        public static event EventHandler CameraPermissionsGranted;
        private View _layout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            _layout = FindViewById(Resource.Id.action_bar_root);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if(requestCode == CameraPermissionsCode && grantResults[0] == Permission.Denied)
            {
                Snackbar.Make(_layout, "Camera permission is denied. Please allow camera use", Snackbar.LengthIndefinite)
                    .SetAction("OK", v => RequestPermissions(CameraPermissions, CameraPermissionsCode))
                    .Show();
                return;
            }

            CameraPermissionsGranted?.Invoke(this, EventArgs.Empty);
        }
    }
}
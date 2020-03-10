using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace CameraApp.Droid.Camera.Listeners
{
    public class CameraStateListener : CameraDevice.StateCallback
    {
        private CameraDroid _CameraDroid;

        public CameraStateListener(CameraDroid cameraDroid)
        {
            _CameraDroid = cameraDroid;
        }
        public override void OnDisconnected(CameraDevice camera)
        {
            if(_CameraDroid == null)
            {
                return;
            }
            else
            {
                camera.Close();
                _CameraDroid.CameraDevice = null;
                _CameraDroid.IsCameraOpening = false;
            }
        }

        public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
        {
            if(_CameraDroid == null)
            {
                return;
            }
            else
            {
                camera.Close();
                _CameraDroid.CameraDevice = null;
                _CameraDroid.IsCameraOpening = false;
            }
        }

        public override void OnOpened(CameraDevice camera)
        {
            if(_CameraDroid == null)
            {
                return;
            }
            else
            {
                _CameraDroid.CameraDevice = camera;
                _CameraDroid.StartPreview();
                _CameraDroid.IsCameraOpening = false;
            }
        }
    }
}
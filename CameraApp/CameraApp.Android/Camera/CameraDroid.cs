using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using CameraApp.CameraView;
using CameraApp.Droid.Camera.Listeners;
using static Android.Resource;

namespace CameraApp.Droid.Camera
{
    public class CameraDroid : FrameLayout, TextureView.ISurfaceTextureListener
    {
        private static readonly SparseIntArray Orientations = new SparseIntArray();

        private Context _context;
        private TextureView _cameraTexture;
        private CameraStateListener _cameraStateListener;

        public bool IsCameraOpening { get; set; }

        private ImageProcessingMode _ImageProcessingMode;
        private CameraManager _manager;
        private Size _previewSize;
        private SurfaceTexture _viewSurface;
        private CaptureRequest.Builder _previewBuilder;
        private CameraCaptureSession _previewSession;

        public event EventHandler<string> PredictionUpdated;

        public CameraDroid(Context context) : base(context)
        {
            _context = context;

            var inflator = LayoutInflater.FromContext(context);

            if(inflator == null)
            {
                return;
            }

            var view = inflator.Inflate(Resource.Layout.CameraLayout, this);

            _cameraTexture = view.FindViewById<TextureView>(Resource.Id.cameraTextureView);

            _cameraTexture.SurfaceTextureListener = this;

            _cameraStateListener = new CameraStateListener(this);

            Orientations.Append((int)SurfaceOrientation.Rotation0, 0);
            Orientations.Append((int)SurfaceOrientation.Rotation90, 90);
            Orientations.Append((int)SurfaceOrientation.Rotation180, 180);
            Orientations.Append((int)SurfaceOrientation.Rotation270, 270);
        }

        public void OpenCamera(CameraOptions cameraOptions, ImageProcessingMode imageProcessingMode)
        {
            if(_context == null || IsCameraOpening)
            {
                return;
            }
            else
            {
                IsCameraOpening = true;
                _ImageProcessingMode = imageProcessingMode;
                _manager = (CameraManager)_context.GetSystemService(Context.CameraService);

                var cameraId = _manager.GetCameraIdList()[(int)cameraOptions];

                var characteristics = _manager.GetCameraCharacteristics(cameraId);
                var map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

                _previewSize = map.GetOutputSizes(Java.Lang.Class.FromType(typeof(SurfaceTexture)))[0];

                _manager.OpenCamera(cameraId, _cameraStateListener, null);
            }
        }

        public CameraDevice CameraDevice { get; internal set; }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _viewSurface = surface;

            configureTransform(width, height);
            StartPreview();
        }

        private void configureTransform(int width, int height)
        {
            if(_viewSurface == null || _previewSize == null || _context == null)
            {
                return;
            }
            else
            {
                var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

                var rotation = windowManager.DefaultDisplay.Rotation;
                var matrix = new Matrix();
                var viewRect = new RectF(0, 0, width, height);
                var bufferRect = new RectF(0, 0, _previewSize.Width, _previewSize.Height);

                var centerX = viewRect.CenterX();
                var centerY = viewRect.CenterY();

                if(rotation == SurfaceOrientation.Rotation90 || rotation == SurfaceOrientation.Rotation270)
                {
                    bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
                    matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);

                    matrix.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
                }

                _cameraTexture.SetTransform(matrix);
            }

        }

        public void StartPreview()
        {
            if(CameraDevice == null || !_cameraTexture.IsAvailable || _previewSize == null)
            {
                return;
            }

            SurfaceTexture texture = _cameraTexture.SurfaceTexture;

            texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);
            Surface previewSurface = new Surface(texture);

            _previewBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
            _previewBuilder.AddTarget(previewSurface);

            var imageReader = ImageReader.NewInstance(_previewSize.Width / 16, _previewSize.Height / 16, ImageFormatType.Yuv420888, 2);
            
            Surface frameCaptureSurface = imageReader.Surface;
            _previewBuilder.AddTarget(frameCaptureSurface);

            var thread = new HandlerThread("CameraPicture");
            thread.Start();
            imageReader.SetOnImageAvailableListener(new ImageAvailableListener(_context, inputString => PredictionUpdated?.Invoke(this, inputString), _ImageProcessingMode), new Handler(thread.Looper));

            CameraDevice.CreateCaptureSession(new List<Surface> { previewSurface, frameCaptureSurface },
                new CameraCaptureStateListener
                {
                    OnConfigureFailedAction = session =>
                    {

                    },
                    OnConfiguredAction = session =>
                    {
                        _previewSession = session;
                        UpdatePreview();
                    }
                }, null);
        }

        private void UpdatePreview()
        {
            if(CameraDevice == null || _previewSession == null)
            {
                return;
            }

            _previewBuilder.Set(CaptureRequest.ControlMode, new Java.Lang.Integer((int)ControlMode.Auto));
            var thread = new HandlerThread("CameraPreview");
            thread.Start();
            var backgroundHandler = new Handler(thread.Looper);

            _previewSession.SetRepeatingRequest(_previewBuilder.Build(), null, backgroundHandler);
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            
        }
    }
}
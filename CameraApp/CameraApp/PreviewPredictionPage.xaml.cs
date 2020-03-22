using CameraApp.CameraView;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CameraApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PreviewPredictionPage : ContentPage
    {
        public string PageTitle
        {
            get { return (string)GetValue(PageTitleProperty); }
            set { SetValue(PageTitleProperty, value); }
        }

        public static readonly BindableProperty PageTitleProperty =
            BindableProperty.Create("PageTitle", typeof(string), typeof(PreviewPredictionPage), "");

        public bool IsPredictionLabelGridVisible
        {
            get { return (bool)GetValue(IsPredictionLabelGridVisibleProperty); }
            set { SetValue(IsPredictionLabelGridVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPredictionLabelGridVisible.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty IsPredictionLabelGridVisibleProperty =
            BindableProperty.Create("IsPredictionLabelGridVisible", typeof(bool), typeof(PreviewPredictionPage), false);



        public ImageProcessingMode ImageProcessingMode
        {
            get { return (ImageProcessingMode)GetValue(ImageProcessingModeProperty); }
            set { SetValue(ImageProcessingModeProperty, value); }
        }

        public static readonly BindableProperty ImageProcessingModeProperty =
            BindableProperty.Create("ImageProcessingMode", typeof(ImageProcessingMode), typeof(PreviewPredictionPage), ImageProcessingMode.JustPreview);


        public PreviewPredictionPage(ImageProcessingMode imageProcessingMode)
        {
            InitializeComponent();
            BindingContext = this;
            ImageProcessingMode = imageProcessingMode;
            switch (imageProcessingMode)
            {
                case ImageProcessingMode.JustPreview:
                    PageTitle = "Camera Preview";
                    IsPredictionLabelGridVisible = false;
                    break;
                case ImageProcessingMode.CatsDetection:
                    PageTitle = "Cats detection";
                    IsPredictionLabelGridVisible = true;
                    break;
                default:
                    break;
            }
        }

        private void SKCanvasView_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintSurfaceEventArgs e)
        {
            SKImageInfo info = e.Info;
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            SKRect rect = new SKRect(0, info.Height - info.Width, info.Width - 2, info.Height - 3);

            canvas.ClipRect(rect);
            canvas.DrawRect(rect, new SKPaint() { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 2 });
        }
    }
}
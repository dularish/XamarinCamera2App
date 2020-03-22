using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CameraApp
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void _justPreviewBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PreviewPredictionPage(CameraView.ImageProcessingMode.JustPreview));
        }

        private async void _catsDetectionPageBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PreviewPredictionPage(CameraView.ImageProcessingMode.CatsDetection));
        }
    }
}

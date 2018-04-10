using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using XLabs;
using XLabs.Ioc;
using XLabs.Platform.Device;
using XLabs.Platform.Services;
using XLabs.Platform.Services.Media;

namespace TestVideoRecord
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}
        async void btnBack_Clicked(object senser, EventArgs e)
        {
            await Navigation.PopAsync(true);
        }

        async void btnTakeVideo_Clicked(object sender, EventArgs e)
        {
            await TakeNewVideo();
        }
        private IMediaPicker _mediaPicker;
        private async Task TakeNewVideo()
        {
            string msg = string.Empty;
            try
            {
                if (_mediaPicker == null)
                {
                    var device = Resolver.Resolve<IDevice>();

                    ////RM: hack for working on windows phone? 
                    _mediaPicker = DependencyService.Get<IMediaPicker>() ?? device.MediaPicker;
                }

                VideoMediaStorageOptions options = new VideoMediaStorageOptions()
                {
                    DefaultCamera = CameraDevice.Rear,
                    Quality = VideoQuality.Medium,
                    DesiredLength = TimeSpan.FromSeconds(20),
                    SaveMediaOnCapture = true
                };

                await _mediaPicker.TakeVideoAsync(options).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        msg = t.Exception.InnerException.ToString();
                    }
                    else if (t.IsCanceled)
                    {
                        msg = "Cancelled";
                    }
                    else
                    {
                        var mediaFile = t.Result;
                        if (mediaFile != null)
                        {
                            LabelResult.Text = "File saved: " + mediaFile.Path;
                        }
                        else
                        {
                            msg = "The video failed to save. Please make sure the device has enough storage and try again.";
                        }
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception x)
            {
                msg = x.Message;
            }

            if (!String.IsNullOrEmpty(msg))
            {
                await DisplayAlert("Failed", msg, "OK");
            }
        }
    }
}

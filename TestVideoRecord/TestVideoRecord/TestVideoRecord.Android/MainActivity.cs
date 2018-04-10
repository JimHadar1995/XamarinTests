using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using XLabs.Ioc;
using XLabs.Platform.Device;
using XLabs.Platform.Services.Media;
using XLabs.Forms.Services;
using XLabs.Serialization;
using XLabs.Platform.Services;

namespace TestVideoRecord.Droid
{
    [Activity(Label = "TestVideoRecord", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);
            if (!Resolver.IsSet)
                SetIoc();
            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }

        /// <summary>
        /// Sets the IoC.
        /// </summary>
        private void SetIoc()
        {
            var resolverContainer = new SimpleContainer();

            resolverContainer.Register<IDevice>(t => AndroidDevice.CurrentDevice)
                .Register<IDisplay>(t => t.Resolve<IDevice>().Display)
                .Register<IFontManager>(t => new FontManager(t.Resolve<IDisplay>()))
                .Register<IMediaPicker, MediaPicker>()
                .Register<ITextToSpeechService, TextToSpeechService>()
                .Register<IDependencyContainer>(resolverContainer)
                .Register<ISecureStorage>(t => new KeyVaultStorage(t.Resolve<IDevice>().Id.ToCharArray()));


            Resolver.SetResolver(resolverContainer.GetResolver());
        }
    }
}

